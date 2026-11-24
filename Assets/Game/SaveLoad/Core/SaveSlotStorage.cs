using System;
using System.IO;
using System.Threading;

namespace RTS.SaveLoad
{
    public enum SaveSlot { Manual, Dawn }
    public enum SaveOperationPhase { Idle, Reading, Writing, Failed }
    public enum SaveCommitStage { TempCreated, TempWritten, TempFlushed, TempVerified, BeforeCommit, Committed }
    public sealed class SaveReadResult
    {
        public readonly MatchSaveData Snapshot;
        public readonly bool UsedBackup;
        public readonly string PrimaryError;
        internal SaveReadResult(MatchSaveData snapshot, bool usedBackup, string primaryError)
        { Snapshot = snapshot; UsedBackup = usedBackup; PrimaryError = primaryError; }
    }
    public sealed class SaveSlotStorage
    {
        readonly string m_Directory;
        readonly SaveCodec m_Codec;
        readonly Action<SaveCommitStage> m_CommitProbe;
        int m_Busy;
        public SaveOperationPhase Phase { get; private set; }
        public string LastError { get; private set; }
        // All service instances pointing at this directory are additionally serialized by an OS file lock.
        public SaveSlotStorage(string directory, SaveCodec codec, Action<SaveCommitStage> commitProbe = null)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("Save directory is required.");
            m_Directory = Path.GetFullPath(directory);
            m_Codec = codec ?? throw new ArgumentNullException(nameof(codec)); m_CommitProbe = commitProbe;
        }
        string PathFor(SaveSlot slot) => Path.Combine(m_Directory, slot switch
        { SaveSlot.Manual => "manual.json", SaveSlot.Dawn => "dawn.json", _ => throw new ArgumentOutOfRangeException(nameof(slot)) });
        void Enter(SaveOperationPhase phase)
        {
            if (Interlocked.CompareExchange(ref m_Busy, 1, 0) != 0) throw new InvalidOperationException("A save operation is already active.");
            Phase = phase; LastError = null;
        }
        void Fail(Exception error) { Phase = SaveOperationPhase.Failed; LastError = error.Message; }
        public SaveReadResult Read(SaveSlot slot)
        {
            string path = PathFor(slot);
            Enter(SaveOperationPhase.Reading);
            try
            {
                // Read does not create, repair or overwrite any file, including corrupt generations.
                using var lease = AcquireReadLease();
                var result = ReadWithBackup(path);
                Phase = SaveOperationPhase.Idle; return result;
            }
            catch (Exception error) { Fail(error); throw; }
            finally { Volatile.Write(ref m_Busy, 0); }
        }
        public void Write(SaveSlot slot, MatchSaveData snapshot)
        {
            string path = PathFor(slot), backup = path + ".bak", temp = path + ".tmp";
            Enter(SaveOperationPhase.Writing);
            try
            {
                Directory.CreateDirectory(m_Directory);
                using var lease = new FileStream(Path.Combine(m_Directory, "slots.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                bool rotatePrimary = true;
                if (File.Exists(path))
                {
                    MatchSaveData previous;
                    try { previous = ReadOne(path); }
                    catch (SaveValidationException)
                    {
                        // A new save after recovery must retain the verified backup, not rotate corruption over it.
                        previous = ReadOne(backup); rotatePrimary = false;
                    }
                    SaveValidator.Require(snapshot != null && snapshot.Generation > previous.Generation, "Slot generation must increase.");
                }
                else if (File.Exists(backup))
                    SaveValidator.Require(snapshot != null && snapshot.Generation > ReadOne(backup).Generation, "Backup generation must increase.");
                byte[] bytes = m_Codec.Encode(snapshot);
                using (var stream = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    m_CommitProbe?.Invoke(SaveCommitStage.TempCreated);
                    stream.Write(bytes, 0, bytes.Length);
                    m_CommitProbe?.Invoke(SaveCommitStage.TempWritten);
                    stream.Flush(true);
                    m_CommitProbe?.Invoke(SaveCommitStage.TempFlushed);
                }
                ReadOne(temp);
                m_CommitProbe?.Invoke(SaveCommitStage.TempVerified);
                m_CommitProbe?.Invoke(SaveCommitStage.BeforeCommit);
                // Same-volume replace retains the previous primary; never delete the primary first.
                if (File.Exists(path)) File.Replace(temp, path, rotatePrimary ? backup : null);
                else File.Move(temp, path);
                m_CommitProbe?.Invoke(SaveCommitStage.Committed);
                Phase = SaveOperationPhase.Idle;
            }
            catch (Exception error) { Fail(error); throw; }
            finally
            {
                // A leftover temp is not a recoverable slot; the next write can replace it.
                Volatile.Write(ref m_Busy, 0);
            }
        }
        FileStream AcquireReadLease()
        {
            string path = Path.Combine(m_Directory, "slots.lock");
            if (!Directory.Exists(m_Directory)) throw new FileNotFoundException("No save exists.");
            // Every writer owns this lock before touching a slot; legacy read-only directories need no new file.
            return File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None) : null;
        }
        SaveReadResult ReadWithBackup(string path)
        {
            try { return new SaveReadResult(ReadOne(path), false, null); }
            catch (Exception error) when (IsReadFailure(error))
            {
                try { return new SaveReadResult(ReadOne(path + ".bak"), true, error.Message); }
                catch (Exception backupError) when (IsReadFailure(backupError))
                { throw new SaveValidationException("No valid save generation. Primary: " + error.Message + " Backup: " + backupError.Message); }
            }
        }
        static bool IsReadFailure(Exception error) => error is IOException || error is UnauthorizedAccessException || error is SaveValidationException;
        MatchSaveData ReadOne(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            SaveValidator.Require(stream.Length > 0 && stream.Length <= SaveValidator.MaxFileBytes, "Invalid save file size.");
            var bytes = new byte[(int)stream.Length];
            int offset = 0;
            while (offset < bytes.Length)
            {
                int count = stream.Read(bytes, offset, bytes.Length - offset);
                if (count == 0) throw new EndOfStreamException("Truncated save.");
                offset += count;
            }
            return m_Codec.Decode(bytes);
        }
    }
}

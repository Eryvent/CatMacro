using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using CatMacro.Models;

namespace CatMacro.Services
{
    public class FileService
    {
        private readonly string _saveDirectory;

        public FileService()
        {
            _saveDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CatMacro");
            if (!Directory.Exists(_saveDirectory))
                Directory.CreateDirectory(_saveDirectory);
        }

        public string GetSavePath(string filename)
        {
            if (!filename.EndsWith(".macro"))
                filename += ".macro";
            return Path.Combine(_saveDirectory, filename);
        }

        public bool SaveRecording(RecordingData recording, string filename)
        {
            try
            {
                var path = GetSavePath(filename);
                var json = recording.ToJson();
                File.WriteAllText(path, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public RecordingData LoadRecording(string filename)
        {
            try
            {
                var path = GetSavePath(filename);
                if (!File.Exists(path))
                    return null;

                var json = File.ReadAllText(path);
                var data = RecordingData.FromJson(json);
                return data ?? new RecordingData { Name = filename, Actions = new() };
            }
            catch
            {
                return null;
            }
        }

        public List<string> GetSavedRecordings()
        {
            try
            {
                return Directory.GetFiles(_saveDirectory, "*.macro")
                    .Select(p => Path.GetFileNameWithoutExtension(p))
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public bool DeleteRecording(string filename)
        {
            try
            {
                var path = GetSavePath(filename);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}

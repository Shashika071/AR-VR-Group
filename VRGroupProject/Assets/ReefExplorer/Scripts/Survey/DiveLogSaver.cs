using System;
using System.IO;
using UnityEngine;

namespace ReefExplorer.Survey
{
    public static class DiveLogSaver
    {
        public static string GetDefaultDirectory()
        {
            return Path.Combine(Application.persistentDataPath, "DiveLogs");
        }

        public static string Save(DiveLogData log, SurveyComparisonResult comparison = null)
        {
            if (log == null)
                throw new ArgumentNullException(nameof(log));

            var directory = GetDefaultDirectory();
            Directory.CreateDirectory(directory);

            var fileName = $"dive_{Sanitize(log.diveId)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            var path = Path.Combine(directory, fileName);

            var payload = new SavedDivePackage
            {
                diveLog = log,
                comparison = comparison,
                savedAtUtc = DateTime.UtcNow.ToString("o"),
                educationalNotice =
                    "Simulated educational dive log. Not a scientific reef health assessment."
            };

            var json = JsonUtility.ToJson(payload, true);
            File.WriteAllText(path, json);
            return path;
        }

        static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "unnamed";

            foreach (var c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value;
        }

        [Serializable]
        sealed class SavedDivePackage
        {
            public DiveLogData diveLog;
            public SurveyComparisonResult comparison;
            public string savedAtUtc;
            public string educationalNotice;
        }
    }
}

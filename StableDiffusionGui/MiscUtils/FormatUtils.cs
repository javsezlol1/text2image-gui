﻿using StableDiffusionGui.Io;
using StableDiffusionGui.Main;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StableDiffusionGui.MiscUtils
{
    internal class FormatUtils
    {
        public static string Bytes(long sizeBytes)
        {
            try
            {
                string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
                if (sizeBytes == 0)
                    return "0" + suf[0];
                long bytes = Math.Abs(sizeBytes);
                int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
                double num = Math.Round(bytes / Math.Pow(1024, place), 1);
                return ($"{Math.Sign(sizeBytes) * num} {suf[place]}");
            }
            catch
            {
                return "N/A B";
            }
        }

        public static string Time(long milliseconds, bool allowMs = true)
        {
            return Time(TimeSpan.FromMilliseconds(milliseconds), allowMs);
        }

        public static string Time(TimeSpan span, bool allowMs = true)
        {
            if (span.TotalHours >= 1f)
                return span.ToString(@"hh\:mm\:ss");

            if (span.TotalMinutes >= 1f)
                return span.ToString(@"mm\:ss");

            if (span.TotalSeconds >= 1f || !allowMs)
                return span.ToString(@"ss".TrimStart('0').PadLeft(1, '0')) + "s";

            return span.ToString(@"fff").TrimStart('0').PadLeft(1, '0') + "ms";
        }

        public static string TimeSw(Stopwatch sw)
        {
            long elapsedMs = sw.ElapsedMilliseconds;
            return Time(elapsedMs);
        }

        public static long TimestampToSecs(string timestamp, bool hasMilliseconds = true)
        {
            try
            {
                string[] values = timestamp.Split(':');
                int hours = int.Parse(values[0]);
                int minutes = int.Parse(values[1]);
                int seconds = int.Parse(values[2].Split('.')[0]);
                long secs = hours * 3600 + minutes * 60 + seconds;

                if (hasMilliseconds)
                {
                    int milliseconds = int.Parse(values[2].Split('.')[1].Substring(0, 2)) * 10;

                    if (milliseconds >= 500)
                        secs++;
                }

                return secs;
            }
            catch (Exception e)
            {
                Logger.Log($"TimestampToSecs({timestamp}) Exception: {e.Message}", true);
                return 0;
            }
        }

        public static long TimestampToMs(string timestamp)
        {
            try
            {
                string[] values = timestamp.Split(':');
                int hours = int.Parse(values[0]);
                int minutes = int.Parse(values[1]);
                int seconds = int.Parse(values[2].Split('.')[0]);
                long ms = 0;

                if (timestamp.Contains("."))
                {
                    int milliseconds = int.Parse(values[2].Split('.')[1].Substring(0, 2)) * 10;
                    ms = hours * 3600000 + minutes * 60000 + seconds * 1000 + milliseconds;
                }
                else
                {
                    ms = hours * 3600000 + minutes * 60000 + seconds * 1000;
                }

                return ms;
            }
            catch (Exception e)
            {
                Logger.Log($"MsFromTimeStamp({timestamp}) Exception: {e.Message}", true);
                return 0;
            }
        }

        public static string SecsToTimestamp(long seconds)
        {
            return (new DateTime(1970, 1, 1)).AddSeconds(seconds).ToString("HH:mm:ss");
        }

        public static string MsToTimestamp(long milliseconds)
        {
            return (new DateTime(1970, 1, 1)).AddMilliseconds(milliseconds).ToString("HH:mm:ss");
        }

        public static string Ratio(long numFrom, long numTo)
        {
            float ratio = ((float)numFrom / (float)numTo) * 100f;
            return ratio.ToString("0.00") + "%";
        }

        public static int RatioInt(long numFrom, long numTo)
        {
            double ratio = Math.Round(((float)numFrom / (float)numTo) * 100f);
            return (int)ratio;
        }

        public static string RatioIntStr(long numFrom, long numTo)
        {
            double ratio = Math.Round(((float)numFrom / (float)numTo) * 100f);
            return ratio + "%";
        }

        public static string ConcatStrings(string[] strings, char delimiter = ',', bool distinct = false)
        {
            string outStr = "";

            strings = strings.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
            if (distinct)
                strings = strings.Distinct().ToArray();

            for (int i = 0; i < strings.Length; i++)
            {
                outStr += strings[i];
                if (i + 1 != strings.Length)
                    outStr += delimiter;
            }

            return outStr;
        }

        public static System.Drawing.Size ParseSize(string str)
        {
            try
            {
                string[] values = str.Split('x');
                return new System.Drawing.Size(values[0].GetInt(), values[1].GetInt());
            }
            catch
            {
                return new System.Drawing.Size();
            }
        }

        public static string CapsIfShort(string codec, int capsIfShorterThan = 5)
        {
            if (codec.Length < capsIfShorterThan)
                return codec.ToUpper();
            else
                return codec.ToTitleCase();
        }

        public static string GetExportFilename (string filePath, string parentDir, string suffix, string ext, int pathLimit, bool includePrompt, bool includeSeed, bool includeScale, bool includeSampler)
        {
            try
            {
                ext = ext.Remove(".");

                var n = DateTime.Now;
                string timestamp = $"{n.Year}-{n.Month.ToString().PadLeft(2, '0')}-{n.Day.ToString().PadLeft(2, '0')}-{n.Hour.ToString().PadLeft(2, '0')}-{n.Minute.ToString().PadLeft(2, '0')}-{n.Second.ToString().PadLeft(2, '0')}";

                int pathBudget = pathLimit - parentDir.Length - timestamp.Length - suffix.Length - 4;

                var meta = IoUtils.GetImageMetadata(filePath);

                string infoStr = "";


                string seed = $"-{meta.Seed}";

                if (includeSeed && (pathBudget - seed.Length > 0))
                {
                    pathBudget -= seed.Length;
                    infoStr += seed;
                }

                string scale = $"-scale{meta.Scale.ToStringDot("0.00")}";

                if (includeScale && (pathBudget - scale.Length > 0))
                {
                    pathBudget -= scale.Length;
                    infoStr += scale;
                }

                string sampler = $"-{meta.Sampler}";

                if (includeSampler && (pathBudget - sampler.Length > 0))
                {
                    pathBudget -= sampler.Length;
                    infoStr += sampler;
                }

                if (includePrompt)
                {
                    return Path.Combine(parentDir, $"{timestamp}{suffix}-{SanitizePromptFilename(meta.Prompt, pathBudget)}{infoStr}") + $".{ext}";
                }
                else
                {
                    return Path.Combine(parentDir, $"{timestamp}{suffix}{infoStr}{suffix}") + $".{ext}";
                }
            }
            catch(Exception ex)
            {
                Logger.Log($"GetExportFilename Error: {ex.Message}\n{ex.StackTrace}");
                return "";
            }
        }

        public static string SanitizePromptFilename (string prompt, int pathBudget = 64)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return "";

            return new Regex(@"[^a-zA-Z0-9 -!,.()]").Replace(prompt, "_").Trunc(pathBudget - 1, false).Replace(" ", "_");
        }
    }
}

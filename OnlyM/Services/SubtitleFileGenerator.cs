﻿namespace OnlyM.Services
{
    using System;
    using System.IO;
    using OnlyM.CoreSys;
    using OnlyM.Models;
    using Serilog;

    internal static class SubtitleFileGenerator
    {
        public static event EventHandler<SubtitleFileEventArgs> SubtitleFileEvent;

        public static string Generate(string mediaItemFilePath, Guid mediaItemId)
        {
            try
            {
                Log.Logger.Debug($"Generating subtitle file for media {mediaItemFilePath}");

                var ffmpegFolder = Unosquare.FFME.MediaElement.FFmpegDirectory;

                var destFolder = Path.GetDirectoryName(mediaItemFilePath);
                if (destFolder == null)
                {
                    return null;
                }

                var srtFileName = Path.GetFileNameWithoutExtension(mediaItemFilePath);
                if (srtFileName == null)
                {
                    return null;
                }

                var videoFileInfo = new FileInfo(mediaItemFilePath);
                if (!videoFileInfo.Exists)
                {
                    return null;
                }

                var srtFile = Path.Combine(destFolder, Path.ChangeExtension(srtFileName, ".srt"));
                if (ShouldCreate(srtFile, videoFileInfo.CreationTimeUtc))
                {
                    SubtitleFileEvent?.Invoke(null, new SubtitleFileEventArgs { MediaItemId = mediaItemId, Starting = true });

                    if (!GraphicsUtils.GenerateSubtitleFile(
                        ffmpegFolder,
                        mediaItemFilePath,
                        srtFile))
                    {
                        return null;
                    }
                    
                    File.SetCreationTimeUtc(srtFile, videoFileInfo.CreationTimeUtc);

                    SubtitleFileEvent?.Invoke(null, new SubtitleFileEventArgs { MediaItemId = mediaItemId, Starting = false });
                }

                return srtFile;
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, $"Could not create srt file for media: {mediaItemFilePath}");
                return null;
            }
        }

        private static bool ShouldCreate(string srtFile, DateTime videoFileCreationTimeUtc)
        {
            var fileInfo = new FileInfo(srtFile);
            if (!fileInfo.Exists)
            {
                return true;
            }

            if (fileInfo.CreationTimeUtc != videoFileCreationTimeUtc)
            {
                Log.Logger.Debug("Old subtitle file found");

                // we also update the subtitles file if it looks
                // like the video has been changed
                return true;
            }

            Log.Logger.Debug("Subtitle file already exists");

            return false;
        }
    }
}

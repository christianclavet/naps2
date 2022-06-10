﻿using Google.Protobuf;
using NAPS2.Scan;

namespace NAPS2.Serialization;

public static class SerializedImageHelper
{
    public static SerializedImage Serialize(ProcessedImage image, SerializeOptions options)
    {
        if (options.RequireFileStorage && options.RequireMemoryStorage)
        {
            throw new ArgumentException();
        }
        if (options.RequireFileStorage && image.Storage is not ImageFileStorage)
        {
            throw new InvalidOperationException("FileStorage is required for serialization.");
        }

        MemoryStream? thumbStream = null;
        var thumb = image.PostProcessingData.Thumbnail;
        if (thumb != null && options.IncludeThumbnail)
        {
            // TODO: Better format choice?
            thumbStream = thumb.SaveToMemoryStream(ImageFileFormat.Png);
        }

        var result = new SerializedImage
        {
            TransferOwnership = options.TransferOwnership,
            Metadata = new SerializedImageMetadata
            {
                TransformListXml = image.TransformState.Transforms.ToXml(),
                BitDepth = (SerializedImageMetadata.Types.BitDepth)image.Metadata.BitDepth,
                Lossless = image.Metadata.Lossless
            },
            Thumbnail = thumbStream != null ? ByteString.FromStream(thumbStream) : ByteString.Empty,
            RenderedFilePath = options.RenderedFilePath ?? ""
        };

        switch (image.Storage)
        {
            case ImageFileStorage fileStorage:
                if (options.RequireMemoryStorage)
                {
                    using var stream = File.OpenRead(fileStorage.FullPath);
                    result.FileContent = ByteString.FromStream(stream);
                }
                else
                {
                    result.FilePath = fileStorage.FullPath;
                }
                break;
            case MemoryStreamImageStorage memoryStreamStorage:
                result.FileContent = ByteString.FromStream(memoryStreamStorage.Stream);
                break;
            case IMemoryImage imageStorage:
                var fileFormat = imageStorage.OriginalFileFormat == ImageFileFormat.Unspecified
                    ? ImageFileFormat.Jpeg
                    : imageStorage.OriginalFileFormat;
                result.FileContent = ByteString.FromStream(imageStorage.SaveToMemoryStream(fileFormat));
                break;
        }
        return result;
    }

    public static ProcessedImage Deserialize(ScanningContext scanningContext, SerializedImage serializedImage, DeserializeOptions options)
    {
        IImageStorage storage;
        if (!string.IsNullOrEmpty(serializedImage.FilePath))
        {
            if (serializedImage.TransferOwnership)
            {
                storage = new ImageFileStorage(serializedImage.FilePath);
            }
            else if (options.ShareFileStorage)
            {
                storage = new ImageFileStorage(serializedImage.FilePath, true);
            }
            else
            {
                // Not transfering or sharing the file, so we need to make a copy
                // TODO: Handle no file storage
                string newPath = scanningContext.FileStorageManager.NextFilePath();
                File.Copy(serializedImage.FilePath, newPath);
                storage = new ImageFileStorage(newPath);
            }
        }
        else
        {
            var memoryStream = new MemoryStream(serializedImage.FileContent.ToByteArray());
            storage = new MemoryStreamImageStorage(memoryStream);
        }

        var processedImage = scanningContext.CreateProcessedImage(
            storage,
            (BitDepth)serializedImage.Metadata.BitDepth,
            serializedImage.Metadata.Lossless,
            -1,
            serializedImage.Metadata.TransformListXml.FromXml<List<Transform>>());

        var thumbnail = serializedImage.Thumbnail.ToByteArray();
        if (thumbnail.Length > 0)
        {
            processedImage = processedImage.WithPostProcessingData(new PostProcessingData
            {
                Thumbnail = scanningContext.ImageContext.Load(new MemoryStream(thumbnail))
            }, true);
        }
        return processedImage;
    }

    public class SerializeOptions
    {
        public bool TransferOwnership { get; set; }

        public bool IncludeThumbnail { get; set; }

        public bool RequireFileStorage { get; set; }

        public bool RequireMemoryStorage { get; set; }

        public string? RenderedFilePath { get; set; }
    }

    public class DeserializeOptions
    {
        /// <summary>
        /// If true, the Deserialize caller guarantees that the file storage will not be used for longer than the duration of the RPC call.
        /// In this way, files can be safely reused even if ownership isn't transferred to the callee.
        /// This should not be true outside of an RPC context.
        /// </summary>
        public bool ShareFileStorage { get; set; }
    }
}

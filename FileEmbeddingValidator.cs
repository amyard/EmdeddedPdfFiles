namespace EmbeddedPdfFiles;

/// <summary>
/// Validates files before embedding them in PDF documents.
/// Provides detailed information about file type limitations.
/// </summary>
public static class FileEmbeddingValidator
{
    // File size limits in bytes
    private const long MAX_PDF_SIZE = 50 * 1024 * 1024; // 50 MB
    private const long MAX_IMAGE_SIZE = 20 * 1024 * 1024; // 20 MB
    private const long MAX_DOCUMENT_SIZE = 30 * 1024 * 1024; // 30 MB
    private const long MAX_ARCHIVE_SIZE = 100 * 1024 * 1024; // 100 MB
    private const long MAX_TEXT_SIZE = 5 * 1024 * 1024; // 5 MB
    private const long MAX_AUDIO_SIZE = 20 * 1024 * 1024; // 20 MB
    
    private const long TOTAL_MAX_SIZE = 200 * 1024 * 1024; // 200 MB total

    /// <summary>
    /// File types that are allowed to be embedded
    /// </summary>
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // PDF
        ".pdf",
        
        // Images
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".tif",
        
        // Documents
        ".docx", ".doc", ".xlsx", ".xls", ".pptx", ".ppt", ".txt", ".rtf", ".csv",
        
        // Archives
        ".zip", ".rar", ".7z",
        
        // Code/Data
        ".json", ".xml", ".cs", ".js", ".py", ".java", ".cpp", ".h", ".html", ".css",
        
        // Audio (with warning)
        ".mp3", ".wav", ".m4a"
    };

    /// <summary>
    /// File types that should never be embedded (security risk)
    /// </summary>
    private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".dll", ".com", ".bat", ".cmd", ".ps1", ".vbs", ".scr", ".msi", ".app", ".jar"
    };

    /// <summary>
    /// File types that generate warnings
    /// </summary>
    private static readonly HashSet<string> WarningExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".m4a", ".mp4", ".avi", ".mov", ".rar", ".7z"
    };

    public enum ValidationResult
    {
        Valid,
        Warning,
        TooLarge,
        UnsupportedType,
        Blocked,
        FileLocked,
        Corrupted,
        PasswordProtected  // ? New validation result
    }

    public class FileValidation
    {
        public ValidationResult Result { get; set; }
        public string Message { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string FileSizeFormatted { get; set; } = string.Empty;
        public long RecommendedMaxSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public bool IsPasswordProtected { get; set; }  // ? New property
        public bool CanEmbed => Result == ValidationResult.Valid || 
                               Result == ValidationResult.Warning ||
                               Result == ValidationResult.PasswordProtected;  // ? Allow encrypted files
    }

    /// <summary>
    /// Validates a file for embedding in PDF
    /// </summary>
    public static FileValidation ValidateFile(string filePath)
    {
        var validation = new FileValidation();

        try
        {
            if (!File.Exists(filePath))
            {
                validation.Result = ValidationResult.Corrupted;
                validation.Message = "File does not exist";
                return validation;
            }

            FileInfo fileInfo = new FileInfo(filePath);
            string extension = fileInfo.Extension.ToLowerInvariant();
            
            validation.FileSize = fileInfo.Length;
            validation.FileSizeFormatted = FormatFileSize(fileInfo.Length);
            validation.FileType = extension;

            // Check if file is blocked
            if (BlockedExtensions.Contains(extension))
            {
                validation.Result = ValidationResult.Blocked;
                validation.Message = $"File type {extension} is blocked for security reasons (executables/scripts not allowed)";
                return validation;
            }

            // Check if file type is allowed
            if (!AllowedExtensions.Contains(extension))
            {
                validation.Result = ValidationResult.UnsupportedType;
                validation.Message = $"File type {extension} is not in the allowed list. Consider converting or skipping.";
                return validation;
            }

            // Determine max size based on file type
            long maxSize = GetMaxSizeForFileType(extension);
            validation.RecommendedMaxSize = maxSize;

            // Check file size
            if (fileInfo.Length > maxSize)
            {
                validation.Result = ValidationResult.TooLarge;
                validation.Message = $"File size ({validation.FileSizeFormatted}) exceeds recommended maximum ({FormatFileSize(maxSize)}) for {extension} files";
                return validation;
            }

            // ? Check if PDF is password-protected
            if (extension == ".pdf")
            {
                bool isEncrypted = IsPdfPasswordProtected(filePath);
                if (isEncrypted)
                {
                    validation.IsPasswordProtected = true;
                    validation.Result = ValidationResult.PasswordProtected;
                    validation.Message = "PDF is password-protected. It will be embedded as-is, but users will need the password to open it after extraction.";
                    return validation;
                }
            }

            // Check if file generates warning
            if (WarningExtensions.Contains(extension))
            {
                validation.Result = ValidationResult.Warning;
                validation.Message = GetWarningMessage(extension);
                return validation;
            }

            // Try to read file to ensure it's accessible
            try
            {
                using FileStream fs = File.OpenRead(filePath);
                // File is accessible
            }
            catch (IOException)
            {
                validation.Result = ValidationResult.FileLocked;
                validation.Message = "File is locked or inaccessible";
                return validation;
            }

            validation.Result = ValidationResult.Valid;
            validation.Message = "File is valid for embedding";
            return validation;
        }
        catch (Exception ex)
        {
            validation.Result = ValidationResult.Corrupted;
            validation.Message = $"Error validating file: {ex.Message}";
            return validation;
        }
    }

    /// <summary>
    /// Checks if a PDF file is password-protected/encrypted
    /// </summary>
    private static bool IsPdfPasswordProtected(string filePath)
    {
        try
        {
            // Read the first 1KB of the file to check PDF header
            byte[] headerBytes = new byte[1024];
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int bytesRead = fs.Read(headerBytes, 0, headerBytes.Length);
                string header = System.Text.Encoding.ASCII.GetString(headerBytes, 0, bytesRead);
                
                // Check for encryption keywords in PDF structure
                // Encrypted PDFs contain /Encrypt dictionary
                if (header.Contains("/Encrypt"))
                {
                    return true;
                }
            }
            
            return false;
        }
        catch
        {
            // If we can't read the file, assume it's not encrypted
            // (actual access error will be caught elsewhere)
            return false;
        }
    }

    /// <summary>
    /// Validates total size of all files
    /// </summary>
    public static (bool isValid, string message) ValidateTotalSize(long totalSize)
    {
        if (totalSize > TOTAL_MAX_SIZE)
        {
            return (false, $"Total embedded file size ({FormatFileSize(totalSize)}) exceeds recommended maximum ({FormatFileSize(TOTAL_MAX_SIZE)})");
        }

        if (totalSize > TOTAL_MAX_SIZE / 2)
        {
            return (true, $"Warning: Total size ({FormatFileSize(totalSize)}) is approaching maximum. PDF may be slow to open.");
        }

        return (true, $"Total size ({FormatFileSize(totalSize)}) is within recommended limits");
    }

    /// <summary>
    /// Gets detailed limitation information for a file type
    /// </summary>
    public static string GetFileLimitations(string extension)
    {
        extension = extension.ToLowerInvariant();

        return extension switch
        {
            ".pdf" => "Max: 50 MB | Best for embedding | Widely supported | ?? Password-protected PDFs: Can embed but users need password to extract",
            ".jpg" or ".jpeg" => "Max: 20 MB | Excellent compatibility | Already compressed | Max 65,535x65,535 pixels",
            ".png" => "Max: 20 MB | Supports transparency | Larger than JPEG | Good for logos/graphics",
            ".gif" => "Max: 10 MB | Animation lost | 256 colors only | Convert to PNG recommended",
            ".bmp" => "Max: 20 MB | Uncompressed | Large files | Convert to PNG/JPEG recommended",
            ".tiff" or ".tif" => "Max: 50 MB | Multi-page support | Not all readers support | Professional use",
            ".docx" or ".doc" => "Max: 30 MB | Good compatibility | Cannot preview in PDF | Must extract to view",
            ".xlsx" or ".xls" => "Max: 50 MB | Spreadsheet data | Large files slow | Consider CSV for data",
            ".pptx" or ".ppt" => "Max: 100 MB | Presentations | Animations lost | Large with images",
            ".txt" => "Max: 5 MB | Smallest size | UTF-8 recommended | Universal compatibility",
            ".rtf" => "Max: 20 MB | Limited formatting | Images as hex (large) | Consider DOCX",
            ".csv" => "Max: 50 MB | Data exchange | Large CSVs slow | Consider splitting",
            ".zip" => "Max: 100 MB | Good compression | Cannot preview | May be blocked by email",
            ".rar" => "Max: 100 MB | Proprietary | Requires special software | Use ZIP instead",
            ".7z" => "Max: 100 MB | Best compression | Requires 7-Zip | Use ZIP for compatibility",
            ".mp3" or ".wav" or ".m4a" => "Max: 20 MB | Cannot play in PDF | Must extract | Large files",
            ".mp4" or ".avi" or ".mov" => "NOT RECOMMENDED | Cannot play in PDF | Very large | Use external links",
            ".json" or ".xml" => "Max: 10 MB | Text-based | Good for config | Large files slow",
            ".cs" or ".js" or ".py" or ".java" or ".cpp" => "Max: 5 MB | Source code | No syntax highlighting | UTF-8 encoding",
            ".exe" or ".dll" or ".bat" or ".ps1" => "BLOCKED | Security risk | Cannot execute | Use download links",
            _ => "Unknown file type | May not be supported | Check compatibility"
        };
    }

    private static long GetMaxSizeForFileType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".pdf" => MAX_PDF_SIZE,
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".bmp" or ".tiff" or ".tif" => MAX_IMAGE_SIZE,
            ".docx" or ".doc" or ".xlsx" or ".xls" or ".pptx" or ".ppt" or ".rtf" or ".csv" => MAX_DOCUMENT_SIZE,
            ".zip" or ".rar" or ".7z" => MAX_ARCHIVE_SIZE,
            ".txt" or ".json" or ".xml" or ".cs" or ".js" or ".py" or ".java" or ".cpp" or ".h" or ".html" or ".css" => MAX_TEXT_SIZE,
            ".mp3" or ".wav" or ".m4a" => MAX_AUDIO_SIZE,
            _ => MAX_DOCUMENT_SIZE // Default
        };
    }

    private static string GetWarningMessage(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".mp3" or ".wav" or ".m4a" => "Audio files cannot be played directly in PDF. Users must extract the file to listen.",
            ".mp4" or ".avi" or ".mov" => "Video files are NOT recommended. They cannot play in PDF and are very large. Consider using external links.",
            ".rar" => "RAR archives require special software to extract. Consider using ZIP for better compatibility.",
            ".7z" => "7Z archives require 7-Zip software. Consider using ZIP for better compatibility.",
            _ => "This file type may have limited support in some PDF readers."
        };
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Gets memory consumption estimate
    /// </summary>
    public static string EstimateMemoryUsage(long totalFileSize, string library)
    {
        double multiplier = library.ToLower() switch
        {
            "itext" => 2.5,
            "aspose" => 1.5,
            _ => 2.0
        };

        long estimatedMemory = (long)(totalFileSize * multiplier);
        return FormatFileSize(estimatedMemory);
    }

    /// <summary>
    /// Prints a summary of all file type limitations
    /// </summary>
    public static void PrintLimitationsSummary()
    {
        Console.WriteLine("\n=== FILE TYPE LIMITATIONS SUMMARY ===\n");
        
        Console.WriteLine("RECOMMENDED (?):");
        Console.WriteLine("  PDF files        : Up to 50 MB each");
        Console.WriteLine("  Images (JPG/PNG) : Up to 20 MB each");
        Console.WriteLine("  Office Documents : Up to 30 MB each");
        Console.WriteLine("  Text/Code files  : Up to 5 MB each");
        
        Console.WriteLine("\nACCEPTABLE WITH CAUTION (??):");
        Console.WriteLine("  Archive files    : Up to 100 MB (ZIP preferred over RAR/7Z)");
        Console.WriteLine("  Audio files      : Up to 20 MB (cannot play in PDF)");
        Console.WriteLine("  Password-protected PDFs: Can embed, users need password to open after extraction");
        
        Console.WriteLine("\nNOT RECOMMENDED (?):");
        Console.WriteLine("  Video files      : Use external links instead");
        Console.WriteLine("  Executables      : Blocked for security reasons");
        Console.WriteLine("  Scripts          : Blocked for security reasons");
        
        Console.WriteLine("\nTOTAL SIZE LIMIT: 200 MB for all embedded files combined");
        Console.WriteLine("MEMORY USAGE: 2-3x total file size (iText) or 1.5-2x (Aspose)");
        Console.WriteLine("\nENCRYPTION NOTE: Password-protected files are embedded as-is.");
        Console.WriteLine("                 Recipients need the password to open extracted files.\n");
    }
}

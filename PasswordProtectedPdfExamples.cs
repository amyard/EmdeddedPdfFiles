using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Filespec;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using System.Text;

namespace EmbeddedPdfFiles;

/// <summary>
/// Demonstrates embedding password-protected PDFs and encrypting output PDFs
/// </summary>
public static class PasswordProtectedPdfExamples
{
    /// <summary>
    /// Example 1: Embed a password-protected PDF (no password needed for embedding)
    /// </summary>
    public static void EmbedPasswordProtectedPdf()
    {
        string outputPath = "Output_WithEncryptedFile.pdf";
        string encryptedFilePath = "Assets/encrypted.pdf"; // Password-protected PDF

        Console.WriteLine("Example 1: Embedding password-protected PDF\n");

        PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

        using (PdfWriter writer = new PdfWriter(outputPath))
        using (PdfDocument pdfDoc = new PdfDocument(writer))
        using (iText.Layout.Document document = new iText.Layout.Document(pdfDoc))
        {
            // Add title
            document.Add(new Paragraph("Embedded Password-Protected Files")
                .SetFont(boldFont)
                .SetFontSize(20));

            document.Add(new Paragraph("This PDF contains an encrypted file. " +
                "You will need the password to open it after extraction.")
                .SetFontSize(12)
                .SetMarginBottom(20));

            // ? Embedding works WITHOUT knowing the password
            // The file is stored as binary data
            try
            {
                byte[] encryptedContent = File.ReadAllBytes(encryptedFilePath);
                
                PdfFileSpec fileSpec = PdfFileSpec.CreateEmbeddedFileSpec(
                    pdfDoc,
                    encryptedContent,
                    "encrypted.pdf",
                    "encrypted.pdf",
                    null,
                    null
                );
                
                pdfDoc.AddFileAttachment("encrypted.pdf", fileSpec);

                document.Add(new Paragraph("? Successfully embedded encrypted.pdf")
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.GREEN));
                document.Add(new Paragraph("?? Password required to open after extraction")
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.ORANGE));

                Console.WriteLine("? Encrypted PDF embedded successfully!");
                Console.WriteLine("??  Users will need the password to open it.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error: {ex.Message}\n");
            }
        }

        Console.WriteLine($"Output saved: {outputPath}\n");
    }

    /// <summary>
    /// Example 2: Try to READ a password-protected PDF (will fail without password)
    /// </summary>
    public static void TryReadPasswordProtectedPdf()
    {
        string encryptedFilePath = "Assets/encrypted.pdf";

        Console.WriteLine("Example 2: Attempting to read password-protected PDF\n");

        // ? This will FAIL - cannot open without password
        try
        {
            using (PdfReader reader = new PdfReader(encryptedFilePath))
            using (PdfDocument pdf = new PdfDocument(reader))
            {
                int pageCount = pdf.GetNumberOfPages();
                Console.WriteLine($"? PDF has {pageCount} pages");
            }
        }
        catch (Exception ex) when (ex.Message.Contains("password") || ex.Message.Contains("Bad password") || ex.Message.Contains("encrypt"))
        {
            Console.WriteLine("? Cannot read: Bad password or file is encrypted");
            Console.WriteLine($"   Exception: {ex.Message}\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error: {ex.Message}\n");
        }

        // ? This works - providing the password
        try
        {
            string password = "your_password_here"; // Replace with actual password
            
            ReaderProperties properties = new ReaderProperties()
                .SetPassword(Encoding.UTF8.GetBytes(password));
                
            using (PdfReader reader = new PdfReader(encryptedFilePath, properties))
            using (PdfDocument pdf = new PdfDocument(reader))
            {
                int pageCount = pdf.GetNumberOfPages();
                Console.WriteLine($"? With correct password: PDF has {pageCount} pages\n");
            }
        }
        catch (Exception ex) when (ex.Message.Contains("password") || ex.Message.Contains("Bad password"))
        {
            Console.WriteLine("? Incorrect password provided\n");
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("??  Encrypted file not found (this is expected if you don't have one)\n");
        }
    }

    /// <summary>
    /// Example 3: Encrypt the OUTPUT PDF (protects all embedded files)
    /// </summary>
    public static void EncryptOutputPdf()
    {
        string outputPath = "Output_Encrypted.pdf";
        
        Console.WriteLine("Example 3: Creating encrypted output PDF\n");

        // User password: Required to open the PDF
        // Owner password: Required to change permissions
        byte[] userPassword = Encoding.UTF8.GetBytes("user123");
        byte[] ownerPassword = Encoding.UTF8.GetBytes("owner456");

        PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

        WriterProperties writerProperties = new WriterProperties()
            .SetStandardEncryption(
                userPassword,
                ownerPassword,
                EncryptionConstants.ALLOW_PRINTING | EncryptionConstants.ALLOW_COPY,
                EncryptionConstants.ENCRYPTION_AES_256
            );

        using (PdfWriter writer = new PdfWriter(outputPath, writerProperties))
        using (PdfDocument pdfDoc = new PdfDocument(writer))
        using (iText.Layout.Document document = new iText.Layout.Document(pdfDoc))
        {
            document.Add(new Paragraph("Encrypted PDF with Embedded Files")
                .SetFont(boldFont)
                .SetFontSize(20));

            document.Add(new Paragraph("This entire PDF is encrypted. " +
                "All embedded files are protected by the PDF password.")
                .SetFontSize(12)
                .SetMarginBottom(20));

            // Embed files (can be unencrypted files - they'll be protected by PDF encryption)
            string sampleFile = "Assets/FIVE_PAGES.pdf";
            if (File.Exists(sampleFile))
            {
                byte[] fileContent = File.ReadAllBytes(sampleFile);
                
                PdfFileSpec fileSpec = PdfFileSpec.CreateEmbeddedFileSpec(
                    pdfDoc,
                    fileContent,
                    "sample.pdf",
                    "sample.pdf",
                    null,
                    null
                );
                
                pdfDoc.AddFileAttachment("sample.pdf", fileSpec);

                document.Add(new Paragraph("? Embedded: sample.pdf (protected by PDF encryption)")
                    .SetFontColor(iText.Kernel.Colors.ColorConstants.GREEN));
            }

            document.Add(new Paragraph("\n?? Encryption Details:")
                .SetFont(boldFont)
                .SetMarginTop(20));
            document.Add(new Paragraph($"User Password: user123"));
            document.Add(new Paragraph($"Owner Password: owner456"));
            document.Add(new Paragraph($"Encryption: AES-256"));
            document.Add(new Paragraph($"Permissions: Print and Copy allowed"));
        }

        Console.WriteLine("? Encrypted PDF created!");
        Console.WriteLine("?? User password: user123");
        Console.WriteLine("?? Owner password: owner456\n");
        Console.WriteLine($"Output saved: {outputPath}\n");
    }

    /// <summary>
    /// Example 4: Best practice - Mixed security levels
    /// </summary>
    public static void MixedSecurityBestPractice()
    {
        string outputPath = "Output_MixedSecurity.pdf";
        
        Console.WriteLine("Example 4: Best practice for mixed security levels\n");

        PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

        using (PdfWriter writer = new PdfWriter(outputPath))
        using (PdfDocument pdfDoc = new PdfDocument(writer))
        using (iText.Layout.Document document = new iText.Layout.Document(pdfDoc))
        {
            document.Add(new Paragraph("Mixed Security Document Package")
                .SetFont(boldFont)
                .SetFontSize(20));

            document.Add(new Paragraph("This package contains both public and confidential files.")
                .SetFontSize(12)
                .SetMarginBottom(20));

            // Table to document which files require passwords
            iText.Layout.Element.Table table = new iText.Layout.Element.Table(3)
                .UseAllAvailableWidth()
                .SetMarginBottom(20);

            // Headers
            table.AddHeaderCell(new iText.Layout.Element.Cell()
                .Add(new Paragraph("File Name").SetFont(boldFont)));
            table.AddHeaderCell(new iText.Layout.Element.Cell()
                .Add(new Paragraph("Security Level").SetFont(boldFont)));
            table.AddHeaderCell(new iText.Layout.Element.Cell()
                .Add(new Paragraph("Password Required").SetFont(boldFont)));

            // Public file
            table.AddCell("public_document.pdf");
            table.AddCell("Public");
            table.AddCell("No");

            // Confidential file (encrypted)
            table.AddCell("confidential.pdf");
            table.AddCell("Confidential");
            table.AddCell("Yes - Contact admin");

            // Internal file (encrypted)
            table.AddCell("internal_only.pdf");
            table.AddCell("Internal");
            table.AddCell("Yes - See password list");

            document.Add(table);

            document.Add(new Paragraph("?? Instructions:")
                .SetFont(boldFont)
                .SetMarginTop(20));
            document.Add(new Paragraph("1. Public files can be opened immediately"));
            document.Add(new Paragraph("2. Confidential files require password (contact document owner)"));
            document.Add(new Paragraph("3. Internal files use standard company password"));
            
            document.Add(new Paragraph("\n?? Security Note:")
                .SetFont(boldFont)
                .SetFontColor(iText.Kernel.Colors.ColorConstants.ORANGE));
            document.Add(new Paragraph(
                "Password-protected files remain encrypted after extraction. " +
                "This PDF itself is not encrypted - only specific embedded files are protected."));
        }

        Console.WriteLine("? Mixed security PDF created!");
        Console.WriteLine("?? Contains documentation about which files need passwords\n");
        Console.WriteLine($"Output saved: {outputPath}\n");
    }

    /// <summary>
    /// Example 5: Detect if a PDF is password-protected
    /// </summary>
    public static void DetectPasswordProtection()
    {
        Console.WriteLine("Example 5: Detecting password-protected PDFs\n");

        string[] testFiles = {
            "Assets/FIVE_PAGES.pdf",
            "Assets/encrypted.pdf"
        };

        foreach (string filePath in testFiles)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"??  {Path.GetFileName(filePath)}: File not found\n");
                continue;
            }

            Console.WriteLine($"Checking: {Path.GetFileName(filePath)}");

            bool isEncrypted = IsPdfEncrypted(filePath);
            
            if (isEncrypted)
            {
                Console.WriteLine("  ?? Status: Password-protected");
                Console.WriteLine("  ??  Can embed: Yes");
                Console.WriteLine("  ??  Can read content: No (without password)");
                Console.WriteLine("  ??  Users will need password after extraction\n");
            }
            else
            {
                Console.WriteLine("  ? Status: Not encrypted");
                Console.WriteLine("  ? Can embed: Yes");
                Console.WriteLine("  ? Can read content: Yes");
                Console.WriteLine("  ? No password needed\n");
            }
        }
    }

    private static bool IsPdfEncrypted(string filePath)
    {
        try
        {
            // Method 1: Check file structure for /Encrypt keyword
            byte[] headerBytes = new byte[2048];
            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                int bytesRead = fs.Read(headerBytes, 0, headerBytes.Length);
                string header = Encoding.ASCII.GetString(headerBytes, 0, bytesRead);
                
                if (header.Contains("/Encrypt"))
                {
                    return true;
                }
            }

            // Method 2: Try to open the PDF
            try
            {
                using (PdfReader reader = new PdfReader(filePath))
                using (PdfDocument pdf = new PdfDocument(reader))
                {
                    // If we can open it without password, it's not encrypted
                    pdf.GetNumberOfPages();
                    return false;
                }
            }
            catch (Exception ex) when (ex.Message.Contains("password") || ex.Message.Contains("Bad password") || ex.Message.Contains("encrypt"))
            {
                return true; // Definitely encrypted
            }

            return false;
        }
        catch
        {
            return false; // If we can't check, assume not encrypted
        }
    }

    /// <summary>
    /// Run all examples
    /// </summary>
    public static void RunAllExamples()
    {
        Console.WriteLine("=".PadRight(70, '='));
        Console.WriteLine("PASSWORD-PROTECTED PDF EXAMPLES");
        Console.WriteLine("=".PadRight(70, '=') + "\n");

        try
        {
            DetectPasswordProtection();
            Console.WriteLine("?".PadRight(70, '?') + "\n");

            EmbedPasswordProtectedPdf();
            Console.WriteLine("?".PadRight(70, '?') + "\n");

            TryReadPasswordProtectedPdf();
            Console.WriteLine("?".PadRight(70, '?') + "\n");

            EncryptOutputPdf();
            Console.WriteLine("?".PadRight(70, '?') + "\n");

            MixedSecurityBestPractice();
            Console.WriteLine("?".PadRight(70, '?') + "\n");

            Console.WriteLine("? All examples completed!\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error running examples: {ex.Message}");
        }
    }
}

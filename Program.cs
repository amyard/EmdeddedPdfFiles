using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Properties;
using iText.Kernel.Pdf.Filespec;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using System.IO;
using Aspose.Pdf;
using Aspose.Pdf.Text;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Colors;
using iText.Layout.Element;
using iText.Layout.Renderer;
using Aspose.Pdf.Annotations;

// Alias for iText types to avoid conflicts
using ITextDocument = iText.Layout.Document;
using ITextTable = iText.Layout.Element.Table;
using ITextCell = iText.Layout.Element.Cell;
using ITextParagraph = iText.Layout.Element.Paragraph;
using ITextLink = iText.Layout.Element.Link;
using ITextRectangle = iText.Kernel.Geom.Rectangle;
using ITextTextAlignment = iText.Layout.Properties.TextAlignment;
using EmdeddedPdfFiles.Models;

Console.WriteLine("=== Embedded PDF Generator ===\n");

var folder = AssetsFolderType.Extensions;
var useLibrary = LibraryProcessType.Both;

string assetsFolder = folder switch
{
    AssetsFolderType.Extensions => "Assets/Extensions",
    AssetsFolderType.HugeBundle => "Assets/HugeBundle",
    AssetsFolderType.HugeDocument => "Assets/HugeDocument",
    _ => throw new NotImplementedException()
};

// Create PDF using iText library
string itextOutputPath = "EmbeddedFiles_iText.pdf";
Console.WriteLine("Creating PDF using iText library...");
if (useLibrary == LibraryProcessType.Itext || useLibrary == LibraryProcessType.Both)
{
    CreateEmbeddedPdfWithIText(assetsFolder, itextOutputPath);
    Console.WriteLine($"✓ PDF created successfully: {itextOutputPath}\n");
}

// Create PDF using Aspose library
string asposeOutputPath = "EmbeddedFiles_Aspose.pdf";
Console.WriteLine("Creating PDF using Aspose library...");
if (useLibrary == LibraryProcessType.Aspose || useLibrary == LibraryProcessType.Both)
{
    CreateEmbeddedPdfWithAspose(assetsFolder, asposeOutputPath);
    Console.WriteLine($"✓ PDF created successfully: {asposeOutputPath}\n");
}

Console.WriteLine("All PDFs created successfully!");

static void CreateEmbeddedPdfWithIText(string assetsFolder, string outputPdfPath)
{
    // Get all files from Assets folder
    if (!Directory.Exists(assetsFolder))
    {
        Console.WriteLine($"Error: {assetsFolder} folder not found!");
        return;
    }

    string[] files = Directory.GetFiles(assetsFolder);
    
    if (files.Length == 0)
    {
        Console.WriteLine($"No files found in {assetsFolder} folder!");
        return;
    }

    // Create PDF writer and document
    using PdfWriter writer = new PdfWriter(outputPdfPath);
    using PdfDocument pdfDoc = new PdfDocument(writer);
    using ITextDocument document = new ITextDocument(pdfDoc);

    // Create fonts
    PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
    PdfFont italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);
    PdfFont regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

    // Add title
    ITextParagraph title = new ITextParagraph("Embedded PDF Files (iText Library)")
        .SetFont(boldFont)
        .SetFontSize(20)
        .SetTextAlignment(ITextTextAlignment.CENTER);
    document.Add(title);

    // Add description
    ITextParagraph description = new ITextParagraph($"This document contains {files.Length} embedded PDF file(s) from the Assets folder. Click the paperclip icons to open files.")
        .SetFontSize(12)
        .SetMarginTop(10)
        .SetMarginBottom(20);
    document.Add(description);

    // Create table for file list
    ITextTable table = new ITextTable(UnitValue.CreatePercentArray(new float[] { 1, 3, 2, 1 }))
        .UseAllAvailableWidth()
        .SetMarginBottom(20);

    // Add table headers
    table.AddHeaderCell(new ITextCell().Add(new ITextParagraph("#").SetFont(boldFont)));
    table.AddHeaderCell(new ITextCell().Add(new ITextParagraph("File Name").SetFont(boldFont)));
    table.AddHeaderCell(new ITextCell().Add(new ITextParagraph("Size").SetFont(boldFont)));
    table.AddHeaderCell(new ITextCell().Add(new ITextParagraph("Open").SetFont(boldFont)));

    // Store file specs and positions for annotations
    var fileAnnotations = new List<(PdfFileSpec fileSpec, int rowIndex)>();

    // Process and embed each file
    int fileIndex = 1;

    foreach (string filePath in files)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        string fileName = fileInfo.Name;
        long fileSize = fileInfo.Length;
        string fileSizeFormatted = FormatFileSize(fileSize);

        // Add file to embedded files in PDF
        byte[] fileContent = File.ReadAllBytes(filePath);
        PdfFileSpec fileSpec = PdfFileSpec.CreateEmbeddedFileSpec(
            pdfDoc,
            fileContent,
            fileName,
            fileName,
            null,
            null
        );
        pdfDoc.AddFileAttachment(fileName, fileSpec);
        fileAnnotations.Add((fileSpec, fileIndex));

        // Add row to table
        table.AddCell(new ITextCell().Add(new ITextParagraph(fileIndex.ToString())));
        table.AddCell(new ITextCell().Add(new ITextParagraph(fileName)));
        table.AddCell(new ITextCell().Add(new ITextParagraph(fileSizeFormatted)));
        
        // Add "📎 Open" text in the action column
        ITextCell actionCell = new ITextCell();
        ITextParagraph actionPara = new ITextParagraph("📎 Open")
            .SetFontColor(ColorConstants.BLUE);
        actionCell.Add(actionPara);
        table.AddCell(actionCell);

        Console.WriteLine($"  Embedded: {fileName} ({fileSizeFormatted})");
        fileIndex++;
    }

    document.Add(table);

    // Add file attachment annotations on the page
    // Position them in the "Open" column for each row
    PdfPage currentPage = pdfDoc.GetPage(1);
    float startY = 530f; // Starting Y position for first row
    float rowHeight = 20f; // Height between rows
    float annotX = 500f; // X position for annotations (Open column)
    float annotWidth = 50f; // Width of annotation area
    float annotHeight = 15f; // Height of annotation
    
    foreach (var (fileSpec, rowIndex) in fileAnnotations)
    {
        float yPos = startY - ((rowIndex - 1) * rowHeight);
        ITextRectangle annotRect = new ITextRectangle(annotX, yPos, annotWidth, annotHeight);
        
        PdfFileAttachmentAnnotation annotation = new PdfFileAttachmentAnnotation(
            annotRect,
            fileSpec
        );
        annotation.SetIconName(new PdfName("Paperclip"));
        annotation.SetContents(new PdfString($"Click to open attachment"));
        annotation.SetColor(ColorConstants.BLUE.GetColorValue());
        
        currentPage.AddAnnotation(annotation);
    }

    // Add instruction note
    ITextParagraph instructionNote = new ITextParagraph("💡 Click on the paperclip icons in the 'Open' column to access files, or use your PDF viewer's attachment panel (View → Attachments).")
        .SetFont(regularFont)
        .SetFontSize(10)
        .SetMarginTop(10)
        .SetMarginBottom(10)
        .SetFontColor(DeviceRgb.BLUE);
    document.Add(instructionNote);

    // Add footer note
    ITextParagraph footer = new ITextParagraph("Note: The files are embedded as attachments in this PDF document.")
        .SetFont(italicFont)
        .SetFontSize(10)
        .SetTextAlignment(ITextTextAlignment.CENTER);
    document.Add(footer);
}

static void CreateEmbeddedPdfWithAspose(string assetsFolder, string outputPdfPath)
{
    // Get all files from Assets folder
    if (!Directory.Exists(assetsFolder))
    {
        Console.WriteLine($"Error: {assetsFolder} folder not found!");
        return;
    }

    string[] files = Directory.GetFiles(assetsFolder);
    
    if (files.Length == 0)
    {
        Console.WriteLine($"No files found in {assetsFolder} folder!");
        return;
    }

    // Create PDF document
    Aspose.Pdf.Document pdfDoc = new Aspose.Pdf.Document();
    Aspose.Pdf.Page page = pdfDoc.Pages.Add();

    // Add title
    TextFragment title = new TextFragment("Embedded PDF Files (Aspose Library)");
    title.TextState.FontSize = 20;
    title.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;
    title.HorizontalAlignment = Aspose.Pdf.HorizontalAlignment.Center;
    page.Paragraphs.Add(title);

    // Add spacing
    page.Paragraphs.Add(new TextFragment(" "));

    // Add description
    TextFragment description = new TextFragment($"This document contains {files.Length} embedded PDF file(s) from the Assets folder. Click the paperclip icons to open files.");
    description.TextState.FontSize = 12;
    page.Paragraphs.Add(description);

    // Add spacing
    page.Paragraphs.Add(new TextFragment(" "));

    // Create table for file list
    Aspose.Pdf.Table table = new Aspose.Pdf.Table
    {
        ColumnWidths = "10% 50% 20% 20%",
        Border = new BorderInfo(BorderSide.All, 0.5f, Aspose.Pdf.Color.Gray),
        DefaultCellBorder = new BorderInfo(BorderSide.All, 0.5f, Aspose.Pdf.Color.LightGray),
        DefaultCellPadding = new MarginInfo(5, 5, 5, 5)
    };

    // Add header row
    Row headerRow = table.Rows.Add();
    headerRow.Cells.Add("#");
    headerRow.Cells.Add("File Name");
    headerRow.Cells.Add("Size");
    headerRow.Cells.Add("Open");
    
    foreach (Aspose.Pdf.Cell headerCell in headerRow.Cells)
    {
        headerCell.BackgroundColor = Aspose.Pdf.Color.LightGray;
        headerCell.DefaultCellTextState.FontStyle = Aspose.Pdf.Text.FontStyles.Bold;
    }

    // Process and embed each file via FileAttachmentAnnotation
    // Using FileAttachmentAnnotation directly embeds the file and makes the paperclip pin clickable on the page
    // WITHOUT duplicate embedding in pdfDoc.EmbeddedFiles.
    int fileIndex = 1;
    var fileAnnotations = new List<(FileSpecification fileSpec, int rowIndex)>();
    
    foreach (string filePath in files)
    {
        FileInfo fileInfo = new FileInfo(filePath);
        string fileName = fileInfo.Name;
        long fileSize = fileInfo.Length;
        string fileSizeFormatted = FormatFileSize(fileSize);

        // Create FileSpecification with Zip compression
        FileSpecification fileSpec = new FileSpecification(filePath, fileName)
        {
            Description = $"Embedded file: {fileName}",
            Encoding = FileEncoding.Zip
        };
        fileAnnotations.Add((fileSpec, fileIndex));

        // Add row to table
        Row row = table.Rows.Add();
        row.Cells.Add(fileIndex.ToString());
        row.Cells.Add(fileName);
        row.Cells.Add(fileSizeFormatted);
        
        // Add "📎 Open" text in the action column
        Aspose.Pdf.Cell actionCell = row.Cells.Add("📎 Open");
        actionCell.DefaultCellTextState.ForegroundColor = Aspose.Pdf.Color.Blue;

        Console.WriteLine($"  Embedded: {fileName} ({fileSizeFormatted})");
        fileIndex++;
    }

    page.Paragraphs.Add(table);

    // Add clickable file attachment annotations on the page
    // Position them in the "Open" column for each row
    double startY = 630; // Starting Y position for first row
    double rowHeight = 22; // Height between rows
    double annotX = 470; // X position for annotations (Open column)
    double annotWidth = 80; // Width of annotation area

    foreach (var (fileSpec, rowIndex) in fileAnnotations)
    {
        double yPos = startY - ((rowIndex - 1) * rowHeight);
        Aspose.Pdf.Rectangle annotRect = new Aspose.Pdf.Rectangle(annotX, yPos - 5, annotX + annotWidth, yPos + 10);

        FileAttachmentAnnotation fileAttachment = new FileAttachmentAnnotation(page, annotRect, fileSpec)
        {
            Icon = FileIcon.Paperclip,
            Color = Aspose.Pdf.Color.Blue,
            Contents = "Click to open attachment"
        };

        page.Annotations.Add(fileAttachment);
    }

    // Add spacing
    page.Paragraphs.Add(new TextFragment(" "));

    // Add instruction note
    TextFragment instructionNote = new TextFragment("💡 Click on the paperclip icons in the 'Open' column to access files, or use your PDF viewer's attachment panel (View → Attachments).");
    instructionNote.TextState.FontSize = 10;
    instructionNote.TextState.ForegroundColor = Aspose.Pdf.Color.Blue;
    page.Paragraphs.Add(instructionNote);

    // Add spacing
    page.Paragraphs.Add(new TextFragment(" "));

    // Add footer note
    TextFragment footer = new TextFragment("Note: The files are embedded as attachments in this PDF document.");
    footer.TextState.FontSize = 10;
    footer.TextState.FontStyle = Aspose.Pdf.Text.FontStyles.Italic;
    footer.HorizontalAlignment = Aspose.Pdf.HorizontalAlignment.Center;
    page.Paragraphs.Add(footer);

    // Optimize document and compress streams
    var optimizationOptions = new Aspose.Pdf.Optimization.OptimizationOptions
    {
        CompressObjects = true,
        RemoveUnusedStreams = true,
        RemoveUnusedObjects = true,
        LinkDuplicateStreams = true,
        AllowReusePageContent = true
    };
    pdfDoc.OptimizeResources(optimizationOptions);

    // Save the document
    pdfDoc.Save(outputPdfPath);
}

static string FormatFileSize(long bytes)
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

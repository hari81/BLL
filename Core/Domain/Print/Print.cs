using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using HiQPdf;
using System.Net;
using System.Text;

namespace BLL.Core.Domain.Print
{
    public class Print
    {

        private string PDF_LICENSE = "7qaHv76K-iKKHjJyP-nJff3sDe-zt/O387Y-39vO3d/A-39zA19fX-1w==";
        public MemoryStream GetPdfFromUrl(List<string[]> cookies, string url)
        {
            MemoryStream dataStream;

            // create the HTML to PDF converter 
            HtmlToPdf htmlToPdfConverter = new HtmlToPdf();
            
            foreach (var cookie in cookies)
                htmlToPdfConverter.HttpCookies.AddCookie(cookie[0], cookie[1]);

            htmlToPdfConverter.ForceResourcesDownload = true;
            htmlToPdfConverter.SerialNumber = PDF_LICENSE;
            // set browser width 
            htmlToPdfConverter.BrowserWidth = 1200;
            htmlToPdfConverter.TriggerMode = ConversionTriggerMode.Manual;
            // set PDF page size and orientation 
            htmlToPdfConverter.Document.PageSize = HiQPdf.PdfPageSize.A4;
            htmlToPdfConverter.Document.PageOrientation = HiQPdf.PdfPageOrientation.Portrait;

            // set PDF page margins 
            htmlToPdfConverter.Document.Margins = new PdfMargins(0);

            // Get the branded URL.
            string undercarriageUrl = "";
            string hostName = HttpContext.Current.Request.Url.Host;
            var brand = new BLL.Core.Domain.Dealership(new DAL.UndercarriageContext()).getDealershipBrandingByHost(hostName, BLL.Core.Domain.InfotrakApplications.UCUI);
            if (brand == null)
            {
                undercarriageUrl = new AppConfigAccess().GetApplicationValue("UCUri");
            }
            else
            {
                undercarriageUrl = "http://" + brand.UCUIHost + "/";
            }

            // Set Header and Footer
            SetHeader(htmlToPdfConverter.Document, undercarriageUrl);
            SetFooter(htmlToPdfConverter.Document, undercarriageUrl);

            ////////////////
            // create the HTTP request
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(undercarriageUrl+url);

            // Set credentials to use for this request
            request.Credentials = CredentialCache.DefaultCredentials;
            request.CookieContainer = new CookieContainer();
            foreach (var cookie in cookies)
                request.CookieContainer.Add(new Cookie(cookie[0],cookie[1], "/" ,domain: "vijaydealership.local"));
            
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            long contentLength = response.ContentLength;
            string contentType = response.ContentType;

            // Get the stream associated with the response
            Stream receiveStream = response.GetResponseStream();

            // Pipes the stream to a higher level stream reader with the required encoding format
            StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8);

            // get the HTML code of the web page
            string htmlCode = readStream.ReadToEnd();

            // close the response and response stream
            response.Close();
            readStream.Close();

            ///////////////

            // set a handler for PageCreatingEvent where to configure the PDF document pages
            htmlToPdfConverter.PageCreatingEvent +=
                        new PdfPageCreatingDelegate(htmlToPdfConverter_PageCreatingEvent);

            try
            {
                // Re-order the pages to move the table of contents to the second page.
                // It was going in front of the cover page.
                htmlToPdfConverter.RunExtensions = true;
                htmlToPdfConverter.ForceResourcesDownload = true;
                htmlToPdfConverter.RunJavaScript = true;
                byte[] pdfBytes = htmlToPdfConverter.ConvertHtmlToMemory(htmlCode, undercarriageUrl);
                dataStream = new MemoryStream(pdfBytes);

            }
            finally
            {
                // dettach from PageCreatingEvent event
                htmlToPdfConverter.PageCreatingEvent -=
                            new PdfPageCreatingDelegate(htmlToPdfConverter_PageCreatingEvent);
            }
            return dataStream;
        }

        public MemoryStream GetPdfFromHtmlText(string HtmlText, int browserWidth)
        {
            MemoryStream dataStream;

            // create the HTML to PDF converter 
            HtmlToPdf htmlToPdfConverter = new HtmlToPdf();
            htmlToPdfConverter.ForceResourcesDownload = true;
            htmlToPdfConverter.SerialNumber = PDF_LICENSE;
            // set browser width 
            htmlToPdfConverter.BrowserWidth = browserWidth;
            htmlToPdfConverter.TriggerMode = ConversionTriggerMode.Auto;
            HtmlText.Replace("~/Api/Application/MainCSS/?InfotrakAppId=3", "");
            // set PDF page size and orientation 
            htmlToPdfConverter.Document.PageSize = HiQPdf.PdfPageSize.A4;
            htmlToPdfConverter.Document.PageOrientation = HiQPdf.PdfPageOrientation.Landscape;

            // set PDF page margins 
            htmlToPdfConverter.Document.Margins = new PdfMargins(0);

            // Get the branded URL.
            string undercarriageUrl = "";
            string hostName = HttpContext.Current.Request.Url.Host;
            var brand = new BLL.Core.Domain.Dealership(new DAL.UndercarriageContext()).getDealershipBrandingByHost(hostName, BLL.Core.Domain.InfotrakApplications.UCUI);
            if (brand == null)
            {
                undercarriageUrl = new AppConfigAccess().GetApplicationValue("UCUri");
            }
            else
            {
                undercarriageUrl = "http://" + brand.UCUIHost + "/";
            }

            // Set Header and Footer
            SetHeader(htmlToPdfConverter.Document, undercarriageUrl);
            SetFooter(htmlToPdfConverter.Document, undercarriageUrl);

            ////////////////
            // create the HTTP request

            // set a handler for PageCreatingEvent where to configure the PDF document pages
            htmlToPdfConverter.PageCreatingEvent +=
                        new PdfPageCreatingDelegate(htmlToPdfConverter_PageCreatingEvent);

            try
            {
                // Re-order the pages to move the table of contents to the second page.
                // It was going in front of the cover page.
                byte[] pdfBytes = htmlToPdfConverter.ConvertHtmlToMemory(HtmlText, undercarriageUrl);
                dataStream = new MemoryStream(pdfBytes);

            }
            finally
            {
                // dettach from PageCreatingEvent event
                htmlToPdfConverter.PageCreatingEvent -=
                            new PdfPageCreatingDelegate(htmlToPdfConverter_PageCreatingEvent);
            }
            return dataStream;
        }

        private void SetHeader(PdfDocumentControl htmlToPdfDocument, string undercarriageUrl)
        {
            // enable header display
            htmlToPdfDocument.Header.Enabled = true;

            // set header height
            htmlToPdfDocument.Header.Height = 20;

            // set header background color
            htmlToPdfDocument.Header.BackgroundColor = System.Drawing.Color.White;
        }

        private void SetFooter(PdfDocumentControl htmlToPdfDocument, string undercarriageUrl)
        {
            // enable footer display
            htmlToPdfDocument.Footer.Enabled = true;

            // set footer height
            htmlToPdfDocument.Footer.Height = 20;

            // set footer background color
            htmlToPdfDocument.Footer.BackgroundColor = System.Drawing.Color.White;
        }

        void htmlToPdfConverter_PageCreatingEvent(PdfPageCreatingParams eventParams)
        {
            PdfPage pdfPage = eventParams.PdfPage;
            int pdfPageNumber = eventParams.PdfPageNumber;

            if (pdfPageNumber == 1)
            {
                // set the header and footer visibility in first page
                pdfPage.DisplayHeader = false;
                pdfPage.DisplayFooter = false;
            }
            else if (pdfPageNumber > 1)
            {
                // set the header and footer visibility in second page
                pdfPage.DisplayHeader = true;
                pdfPage.DisplayFooter = true;
            }
        }

        private static string sample = @"
<html>
<head>
    <title>Conversion Triggering Mode</title>
</head>
<body>
    <br />
    <br />
    <span style=""font-family: Times New Roman; font-size: 10pt"">When the triggering mode
        is 'Manual' the conversion is triggered by the call to<b> hiqPdfConverter.startConversion()</b>
        from JavaScript.<br />
        In this example document the startConversion() method is called when the ticks count
        reached 100 which happens in about 3 seconds.</span>
    <br />
    <br />
    <b>Ticks Count:</b> <span style = ""color: Red"" id=""ticks"">0</span>
    <br />
    <br />
    <!-- display HiQPdf HTML converter version if the document is loaded in converter-->
    <span style = ""font-family: Times New Roman; font-size: 10pt"" > HiQPdf Info:
        <script type = ""text/javascript"" >
            // check if the document is loaded in HiQPdf HTML to PDF Converter
            if (typeof hiqPdfInfo == ""undefined"") {
                // hiqPdfInfo object is not defined and the document is loaded in a browser
                document.write(""Not in HiQPdf"");
            }
            else {
                // hiqPdfInfo object is defined and the document is loaded in converter
                document.write(hiqPdfInfo.getVersion());
            }
        </script>
    </span>
    <br />
    <script type = ""text/javascript"" >
        var ticks = 0;
        function tick()
{
    // increment ticks count
    ticks++;

    var ticksElement = document.getElementById(""ticks"");
    // set ticks count
    ticksElement.innerHTML = ticks;
    if (ticks == 100)
    {
        // trigger conversion
        ticksElement.style.color = ""green"";
        if (typeof hiqPdfConverter != ""undefined"")
            hiqPdfConverter.startConversion();
    }
    else
    {
        // wait one more tick
        setTimeout(""tick()"", 30);
    }
}

        tick();
    </script>
</body>
</html>
";

    }
}
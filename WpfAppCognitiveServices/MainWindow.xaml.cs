using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;

namespace WpfAppCognitiveServices
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string clientKey = "";

        private readonly IFaceServiceClient objFaceServiceCilent = new FaceServiceClient(clientKey);

        private List<Guid> faceIds = new List<Guid>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";

            bool? result = openDlg.ShowDialog(this);

            if (!(bool)result)
                return;

            var filePath = openDlg.FileName;

            var fileUri = new Uri(filePath);

            var bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            imgPhoto.Source = bitmapSource;

            imgAttributes.Clear();
            faceIds.Clear();

            Title = "Detecting...";

            Face[] faces = await UploadAndDetectFaces(filePath);

            FaceRectangle[] faceRects = faces.Select(face => face.FaceRectangle).ToArray();

            Title = String.Format("Detection Finished. {0} face(s) detected", faceRects.Length);

            if (faceRects.Length > 0)
            {
                DrawingVisual visual = new DrawingVisual();
                DrawingContext drawingContext = visual.RenderOpen();
                drawingContext.DrawImage(bitmapSource,
                    new Rect(0, 0, bitmapSource.Width, bitmapSource.Height));
                double dpi = bitmapSource.DpiX;
                double resizeFactor = 96 / dpi;

                foreach (var faceRect in faceRects)
                {
                    drawingContext.DrawRectangle(
                        Brushes.Transparent,
                        new Pen(Brushes.Red, 10),
                        new Rect(
                            faceRect.Left * resizeFactor,
                            faceRect.Top * resizeFactor,
                            faceRect.Width * resizeFactor,
                            faceRect.Height * resizeFactor
                            )
                    );
                }

                drawingContext.Close();
                RenderTargetBitmap faceWithRectBitmap = new RenderTargetBitmap(
                    (int)(bitmapSource.PixelWidth * resizeFactor),
                    (int)(bitmapSource.PixelHeight * resizeFactor),
                    96,
                    96,
                    PixelFormats.Pbgra32);

                faceWithRectBitmap.Render(visual);

                imgPhoto.Source = faceWithRectBitmap;

                var i = 1;

                foreach(var face in faces)
                {
                    imgAttributes.Text += string.Format("Face {0} - {4}{2}Gender: {3} Age: {1}{2}{2}", i, face.FaceAttributes.Age, Environment.NewLine, face.FaceAttributes.Gender, face.FaceId.ToString());
                    faceIds.Add(face.FaceId);
                    i += 1;
                }
            }
        }

        private async Task<Face[]> UploadAndDetectFaces(string imageFilePath)
        {
            try
            {
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    var requiredFaceAttributes = new FaceAttributeType[] { FaceAttributeType.Age, FaceAttributeType.Gender, FaceAttributeType.Accessories };
                    var faces = await objFaceServiceCilent.DetectAsync(imageFileStream, true, false, requiredFaceAttributes);
                    
                    return faces.ToArray();
                }
            }
            catch (Exception)
            {
                return new Face[0];
            }
        }

        private async void btnMathc_Click(object sender, RoutedEventArgs e)
        {
            bool isMatched = false;

            var openDlg = new Microsoft.Win32.OpenFileDialog();

            openDlg.Filter = "JPEG Image(*.jpg)|*.jpg";

            bool? result = openDlg.ShowDialog(this);

            if (!(bool)result)
                return;

            var filePath = openDlg.FileName;

            var fileUri = new Uri(filePath);

            var bitmapSource = new BitmapImage();

            bitmapSource.BeginInit();
            bitmapSource.CacheOption = BitmapCacheOption.None;
            bitmapSource.UriSource = fileUri;
            bitmapSource.EndInit();

            imgMatch.Source = bitmapSource;

            Face[] faces = await UploadAndDetectFaces(filePath);

            foreach (var face in faces)
            {
                foreach (var fid in faceIds)
                {
                    var vResult = await objFaceServiceCilent.VerifyAsync(fid, face.FaceId);

                    if(vResult.IsIdentical)
                    {
                        imgAttributes.Text += string.Format("Positive face match on ID {0}{2}Confidence: {1}{2}{2}", fid.ToString(), vResult.Confidence.ToString(), Environment.NewLine);
                        isMatched = true;
                    }
                }
            }

            if (!isMatched)
                imgAttributes.Text += "No match found.";
        }
    }
}

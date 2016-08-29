using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.UI.Xaml.Controls;

namespace WebCamSample
{
    public class Webcam
    {
        private readonly MediaCapture mediaCapture;

        public Webcam()
        {
            mediaCapture = new MediaCapture();

            //initialize().Wait();

            //element.Source = mediaCapture;
        }

        public async Task Capture(ImageEncodingProperties imageProperties, IStorageFile photoFile)
        {
            await mediaCapture.CapturePhotoToStorageFileAsync(imageProperties, photoFile);
        }
        public async Task StartPreviewAsync()
        {
            await mediaCapture.StartPreviewAsync();
        }

        public async Task StopPreviewAsync()
        {
            await mediaCapture.StopPreviewAsync();
        }

        public async Task StopRecordAsync()
        {
            await mediaCapture.StopRecordAsync();
        }

        public async Task Initialize(CaptureElement element)
        {
            await mediaCapture.InitializeAsync();
            element.Source = mediaCapture;
        }
    }
}

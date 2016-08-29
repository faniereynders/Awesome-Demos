/*
    Copyright(c) Microsoft Open Technologies, Inc. All rights reserved.

    The MIT License(MIT)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Streams;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;

namespace WebCamSample
{
    


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //private MediaCapture mediaCapture;
        private Webcam webcam;
        private StorageFile photoFile;
        private StorageFile recordStorageFile;
        private StorageFile audioFile;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";
        private readonly string VIDEO_FILE_NAME = "video.mp4";
        private readonly string AUDIO_FILE_NAME = "audio.mp3";
        private bool isPreviewing;
        private bool isRecording;
        private string _blobStorageConnectionString = "DefaultEndpointsProtocol=https;AccountName=faniestorage;AccountKey=aFy7zOM7g3woklnGkT13qp8N4un6vxkkiBbun6Ucxp2TDF4k4fJm2zTllPGC3MG3gTl+zR+Z4LT8y6bIDlBgiA==";

        private readonly SynchronizationContext synchronizationContext;
        private bool isTracking = false;
        #region HELPER_FUNCTIONS

        enum Action
        {
            ENABLE,
            DISABLE
        }
        /// <summary>
        /// Helper function to enable or disable Initialization buttons
        /// </summary>
        /// <param name="action">enum Action</param>
        

        /// <summary>
        /// Helper function to enable or disable video related buttons (TakePhoto, Start Video Record)
        /// </summary>
        /// <param name="action">enum Action</param>
        private void SetVideoButtonVisibility(Action action)
        {
            if (action == Action.ENABLE)
            {
                takePhoto.IsEnabled = true;
                takePhoto.Visibility = Visibility.Visible;

                //recordVideo.IsEnabled = true;
                //recordVideo.Visibility = Visibility.Visible;
            }
            else
            {
                takePhoto.IsEnabled = false;
                takePhoto.Visibility = Visibility.Collapsed;

                //recordVideo.IsEnabled = false;
                //recordVideo.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Helper function to enable or disable audio related buttons (Start Audio Record)
        /// </summary>
        /// <param name="action">enum Action</param>
        private void SetAudioButtonVisibility(Action action)
        {
            if (action == Action.ENABLE)
            {
                //recordAudio.IsEnabled = true;
                //recordAudio.Visibility = Visibility.Visible;
            }
            else
            {
                //recordAudio.IsEnabled = false;
                //recordAudio.Visibility = Visibility.Collapsed;
            }
        }
        #endregion
        public MainPage()
        {
            this.InitializeComponent();

            webcam = new Webcam();

            //SetInitButtonVisibility(Action.ENABLE);
            SetVideoButtonVisibility(Action.DISABLE);
            SetAudioButtonVisibility(Action.DISABLE);

            isRecording = false;
            isPreviewing = false;
            synchronizationContext = SynchronizationContext.Current;

            
        }

        private async Task receiveCommands()
        {
            try
            {
                while (true)
                {
                    var message = await AzureIoTHub.ReceiveCloudToDeviceMessageAsync();
                    switch (message.Command)
                    {
                        case "start-track":
                            synchronizationContext.Post((o) =>
                            {
                                takePhoto_Click(this, null);

                            }, null);
                            break;
                        case "stop-track":
                            isTracking = false;
                            synchronizationContext.Post((o) =>
                            {
                                status.Text = "Tracking stopped.";
                            }, null);
                            break;
                        case "set-color":
                            await Task.Run(()=> LightController.SetColor(message.Payload, false));
                            break;
                        case "flash-color":
                            await Task.Run(()=> LightController.SetColor(message.Payload, true));
                            break;
                        default:
                            break;
                    }

                }
            }
            catch (Exception ex)
            {

                throw;
            }
            
        }

        private async void Cleanup()
        {
            if (webcam != null)
            {
                // Cleanup MediaCapture object
                if (isPreviewing)
                {
                    await webcam.StopPreviewAsync();
                    captureImage.Source = null;
                   // playbackElement.Source = null;
                    isPreviewing = false;
                }
                if (isRecording)
                {
                    await webcam.StopRecordAsync();
                    isRecording = false;
                    //recordVideo.Content = "Start Video Record";
                    //recordAudio.Content = "Start Audio Record";
                }                
               // mediaCapture.Dispose();
              //  mediaCapture = null;
            }
            //SetInitButtonVisibility(Action.ENABLE);
        }

        /// <summary>
        /// 'Initialize Audio and Video' button action function
        /// Dispose existing MediaCapture object and set it up for audio and video
        /// Enable or disable appropriate buttons
        /// - DISABLE 'Initialize Audio and Video' 
        /// - DISABLE 'Start Audio Record'
        /// - ENABLE 'Initialize Audio Only'
        /// - ENABLE 'Start Video Record'
        /// - ENABLE 'Take Photo'
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async Task startWebcamPreview()
        {
            // Disable all buttons until initialization completes

          //  SetInitButtonVisibility(Action.DISABLE);
            SetVideoButtonVisibility(Action.DISABLE);
            SetAudioButtonVisibility(Action.DISABLE);

            try
            {
                if (webcam != null)
                {
                    // Cleanup MediaCapture object
                    if (isPreviewing)
                    {
                        await webcam.StopPreviewAsync();
                        captureImage.Source = null;
                      //  playbackElement.Source = null;
                        isPreviewing = false;
                    }
                    if (isRecording)
                    {
                        await webcam.StopRecordAsync();
                        isRecording = false;
                        //recordVideo.Content = "Start Video Record";
                        //recordAudio.Content = "Start Audio Record";
                    }
                    //mediaCapture.Dispose();
                    //mediaCapture = null;
                }

                status.Text = "Initializing camera to capture audio and video...";
                // Use default initialization
                //mediaCapture = new MediaCapture();
              //  await we.InitializeAsync();                

                // Set callbacks for failure and recording limit exceeded
                status.Text = "Device successfully initialized for video recording!";
               // mediaCapture.Failed += new MediaCaptureFailedEventHandler(mediaCapture_Failed);
               // mediaCapture.RecordLimitationExceeded += new Windows.Media.Capture.RecordLimitationExceededEventHandler(mediaCapture_RecordLimitExceeded);

                // Start Preview                
              //  previewElement.Source = mediaCapture;
                await webcam.StartPreviewAsync();
                isPreviewing = true;
                status.Text = "Camera preview succeeded";

                // Enable buttons for video and photo capture
                SetVideoButtonVisibility(Action.ENABLE);

                // Enable Audio Only Init button, leave the video init button disabled
              //  audio_init.IsEnabled = true;
            }
            catch (Exception ex)
            {
                status.Text = "Unable to initialize camera for audio/video mode: " + ex.Message;             
            }
        }

        private void cleanup_Click(object sender, RoutedEventArgs e)
        {
        //    SetInitButtonVisibility(Action.DISABLE);
            SetVideoButtonVisibility(Action.DISABLE);
            SetAudioButtonVisibility(Action.DISABLE);
            Cleanup();            
        }

        
        /// <summary>
        /// 'Initialize Audio Only' button action function
        /// Dispose existing MediaCapture object and set it up for audio only
        /// Enable or disable appropriate buttons
        /// - DISABLE 'Initialize Audio Only' 
        /// - DISABLE 'Start Video Record'
        /// - DISABLE 'Take Photo'
        /// - ENABLE 'Initialize Audio and Video'
        /// - ENABLE 'Start Audio Record'        
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        

        private async Task<Emotion[]> UploadAndDetectEmotions(string imageFilePath)
        {
            
            string subscriptionKey = "85c1dff503d54109bffa0e1fe8072059";

            
            var emotionServiceClient = new EmotionServiceClient(subscriptionKey);

           
            try
            {
                Emotion[] emotionResult;
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    //
                    // Detect the emotions in the URL
                    //
                    emotionResult = await emotionServiceClient.RecognizeAsync(imageFileStream);
                    return emotionResult;
                }
            }
            catch (Exception exception)
            {
               
                return null;
            }
            // -----------------------------------------------------------------------
            // KEY SAMPLE CODE ENDS HERE
            // -----------------------------------------------------------------------

        }
        private async Task<CloudBlobContainer> getImagesBlobContainer()
        {
            // use the connection string to get the storage account
            var storageAccount = CloudStorageAccount.Parse(_blobStorageConnectionString);
            // using the storage account, create the blob client
            var blobClient = storageAccount.CreateCloudBlobClient();
            // finally, using the blob client, get a reference to our container
            var container = blobClient.GetContainerReference("demo");
            
            // by default, blobs are private and would require your access key to download.
            //   You can allow public access to the blobs by making the container public.   
            //await container.SetPermissionsAsync(
            //    new BlobContainerPermissions
            //    {
            //        PublicAccess = BlobContainerPublicAccessType.Blob
            //    });
            return container;
        }
        //private async Task<string> uploadPhoto(string name, byte[] fileBytes)
        //{
        //    var container = await getImagesBlobContainer();
        //    // using the container reference, get a block blob reference and set its type
        //    var blockBlob = container.GetBlockBlobReference(name);
        //    blockBlob.Properties.ContentType = "image/png";
        //    // finally, upload the image into blob storage using the block blob reference
        //  //  var fileBytes = image.Data;
        //    await blockBlob.UploadFromByteArrayAsync(fileBytes, 0, fileBytes.Length);
        //    return blockBlob.Uri.ToString();
        //}

        /// <summary>
        /// 'Take Photo' button click action function
        /// Capture image to a file in the default account photos folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void takePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                status.Text = "Tracking started";
                isTracking = true;
                string lastEmotion = string.Empty;
                while (isTracking)
                {
                    takePhoto.IsEnabled = false;
                    //recordVideo.IsEnabled = false;
                    captureImage.Source = null;

                    photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                        PHOTO_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                    ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                    await webcam.Capture(imageProperties, photoFile);
                    takePhoto.IsEnabled = true;
                    status.Text = "Take Photo succeeded: " + photoFile.Path;
                    

                    await Task.Run(async () =>
                    {
                        
                        //var imageUrl = await uploadPhoto($"{Guid.NewGuid().ToString()}.png", File.ReadAllBytes(photoFile.Path));
                        var emotions = await UploadAndDetectEmotions(photoFile.Path);
                        var results = new List<EmotionalEvent>();
                        foreach (var item in emotions)
                        {
                            var emotionData = new Dictionary<string, float>();
                            emotionData.Add(nameof(item.Scores.Anger), item.Scores.Anger);
                            emotionData.Add(nameof(item.Scores.Contempt), item.Scores.Contempt);
                            emotionData.Add(nameof(item.Scores.Disgust), item.Scores.Disgust);
                            emotionData.Add(nameof(item.Scores.Fear), item.Scores.Fear);
                            emotionData.Add(nameof(item.Scores.Happiness), item.Scores.Happiness);
                            emotionData.Add(nameof(item.Scores.Sadness), item.Scores.Sadness);
                            emotionData.Add(nameof(item.Scores.Surprise), item.Scores.Surprise);
                            var result = emotionData
                                       //.Where(_ => _.Value <= 1)
                                       .OrderByDescending(_ => _.Value)
                                       .Select(_ => new EmotionalEvent
                                       {
                                           Emotion = _.Key,
                                       // date = DateTime.Now
                                   }).FirstOrDefault();
                            if (result != null)
                            {
                                results.Add(result);
                            }
                            
                        }

                        var topEmotion = results.GroupBy(_ => _.Emotion).OrderByDescending(_ => _.Count()).Select(_=>_.Key).FirstOrDefault();

                        if (lastEmotion != topEmotion && topEmotion != null)
                        {
                            lastEmotion = topEmotion;

                            synchronizationContext.Post((v) =>
                            {
                               
                                    emotion.Text = v.ToString();
                                
                                
                            }, lastEmotion);

                            await sendEmotionUpdate(lastEmotion);
                        }

                        if (results.Count > 0)
                        {
                           await AzureIoTHub.SendToHubAsync(results);
                        }
                        
                    });

                    // var emotions = await Task.Run<Emotion[]>(async () =>
                    //{
                    //    return await UploadAndDetectEmotions(photoFile.Path);
                    //});

                    // var d = new Dictionary<string, float>();

                    // d.Add("Angry", emotions[0].Scores.Anger);
                    // d.Add("Contempt", emotions[0].Scores.Contempt);
                    // d.Add("Disgusted", emotions[0].Scores.Disgust);
                    // d.Add("Fearful", emotions[0].Scores.Fear);
                    // d.Add("Happy", emotions[0].Scores.Happiness);
                    // d.Add("Neutral", emotions[0].Scores.Neutral);
                    // d.Add("Sad", emotions[0].Scores.Sadness);
                    // d.Add("Surprised", emotions[0].Scores.Surprise);


                    // var results = d.Where(_ => _.Value <= 1).OrderByDescending(_ => _.Value);
                    // lblEmotion.Text = results.Select(_ => _.Key).FirstOrDefault();


                    IRandomAccessStream photoStream = await photoFile.OpenReadAsync();
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.SetSource(photoStream);
                    captureImage.Source = bitmap;
                    await Task.Delay(5000);
                }

                
            }
            catch (Exception ex)
            {
                status.Text = ex.Message;
                Cleanup();
            }
            finally
            {
                takePhoto.IsEnabled = true;
                //recordVideo.IsEnabled = true;
            }
        }

        private async Task sendEmotionUpdate(string emotion)
        {
            using (var client = new HttpClient())
            {
                var msg = new
                {
                    type = "EmotionUpdate",
                    data = emotion
                };
                var content = new StringContent(JsonConvert.SerializeObject(msg), Encoding.UTF8, "application/json");
                client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "76677ee160bf4f14af9c8ca23511b9a3");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "QXp1cmVCb290Y2FtcEJvdDo3NjY3N2VlMTYwYmY0ZjE0YWY5YzhjYTIzNTExYjlhMw==");
                var result = await client.PostAsync("https://awesomebot.azurewebsites.net/api/webhooks", content);
            }
        }

        /// <summary>
        /// 'Start Video Record' button click action function
        /// Button name is changed to 'Stop Video Record' once recording is started
        /// Records video to a file in the default account videos folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
       

        /// <summary>
        /// 'Start Audio Record' button click action function
        /// Button name is changes to 'Stop Audio Record' once recording is started
        /// Records audio to a file in the default account video folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        

        /// <summary>
        /// Callback function for any failures in MediaCapture operations
        /// </summary>
        /// <param name="currentCaptureObject"></param>
        /// <param name="currentFailure"></param>
        private async void mediaCapture_Failed(MediaCapture currentCaptureObject, MediaCaptureFailedEventArgs currentFailure)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                try
                {
                    status.Text = "MediaCaptureFailed: " + currentFailure.Message;

                    if (isRecording)
                    {
                        await webcam.StopRecordAsync();
                        status.Text += "\n Recording Stopped";
                    }
                }
                catch (Exception)
                {
                }
                finally
                {
             //       SetInitButtonVisibility(Action.DISABLE);
                    SetVideoButtonVisibility(Action.DISABLE);
                    SetAudioButtonVisibility(Action.DISABLE);
                    status.Text += "\nCheck if camera is diconnected. Try re-launching the app";                    
                }
            });            
        }

        /// <summary>
        /// Callback function if Recording Limit Exceeded
        /// </summary>
        /// <param name="currentCaptureObject"></param>
        public async void mediaCapture_RecordLimitExceeded(Windows.Media.Capture.MediaCapture currentCaptureObject)
        {
            try
            {
                if (isRecording)
                {
                    await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        try
                        {
                            status.Text = "Stopping Record on exceeding max record duration";
                            await webcam.StopRecordAsync();
                            isRecording = false;
                            //recordAudio.Content = "Start Audio Record";
                            //recordVideo.Content = "Start Video Record";
                           
                        }
                        catch (Exception e)
                        {
                            status.Text = e.Message;
                        }
                    });
                }
            }
            catch (Exception e)
            {
                status.Text = e.Message;
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await webcam.Initialize(previewElement);
            startWebcam_Click(this, null);
            await receiveCommands();
        }

        private async void startWebcam_Click(object sender, RoutedEventArgs e)
        {
            await startWebcamPreview();
            isTracking = true;
            startWebcam.Visibility = Visibility.Collapsed;
            takePhoto.Visibility = Visibility.Visible;
            stopWebcam.Visibility = Visibility.Visible;

        }

        private async void stopWebcam_Click(object sender, RoutedEventArgs e)
        {
            await webcam.StopPreviewAsync();
            isPreviewing = false;
            isTracking = false;
            startWebcam.Visibility = Visibility.Visible;
            takePhoto.Visibility = Visibility.Collapsed;
            stopWebcam.Visibility = Visibility.Collapsed;
        }
    }
}

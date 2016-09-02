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
using System.Threading;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace WebCamSample
{
    public sealed partial class MainPage : Page
    {
        private Webcam webcam;
        private StorageFile photoFile;
        private StorageFile recordStorageFile;
        private StorageFile audioFile;
        private readonly string PHOTO_FILE_NAME = "photo.jpg";
        private readonly string VIDEO_FILE_NAME = "video.mp4";
        private readonly string AUDIO_FILE_NAME = "audio.mp3";
        private bool isPreviewing;
        private bool isRecording;
       
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
               
                if (isPreviewing)
                {
                    await webcam.StopPreviewAsync();
                    captureImage.Source = null;
                   
                    isPreviewing = false;
                }
                if (isRecording)
                {
                    await webcam.StopRecordAsync();
                    isRecording = false;
                    
                }                
               
            }
            
        }

        private async Task startWebcamPreview()
        {
          
            SetVideoButtonVisibility(Action.DISABLE);
            SetAudioButtonVisibility(Action.DISABLE);

            try
            {
                if (webcam != null)
                {
                    
                    if (isPreviewing)
                    {
                        await webcam.StopPreviewAsync();
                        captureImage.Source = null;
                      
                        isPreviewing = false;
                    }
                    if (isRecording)
                    {
                        await webcam.StopRecordAsync();
                        isRecording = false;
                        
                    }
                    
                }

                status.Text = "Initializing camera to capture audio and video...";
                
                status.Text = "Device successfully initialized for video recording!";
              
                await webcam.StartPreviewAsync();
                isPreviewing = true;
                status.Text = "Camera preview succeeded";

               
                SetVideoButtonVisibility(Action.ENABLE);

                
            }
            catch (Exception ex)
            {
                status.Text = "Unable to initialize camera for audio/video mode: " + ex.Message;             
            }
        }
        
        private async Task<Emotion[]> UploadAndDetectEmotions(string imageFilePath)
        {
            
            string subscriptionKey = App.Configuration.MsCognitiveEmotion;

            
            var emotionServiceClient = new EmotionServiceClient(subscriptionKey);

           
            try
            {
                Emotion[] emotionResult;
                using (Stream imageFileStream = File.OpenRead(imageFilePath))
                {
                    emotionResult = await emotionServiceClient.RecognizeAsync(imageFileStream);
                    return emotionResult;
                }
            }
            catch (Exception exception)
            {
               
                return null;
            }
            
        }
        
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
                    
                    captureImage.Source = null;

                    photoFile = await KnownFolders.PicturesLibrary.CreateFileAsync(
                        PHOTO_FILE_NAME, CreationCollisionOption.ReplaceExisting);
                    ImageEncodingProperties imageProperties = ImageEncodingProperties.CreateJpeg();
                    await webcam.Capture(imageProperties, photoFile);
                    takePhoto.IsEnabled = true;
                    status.Text = "Take Photo succeeded: " + photoFile.Path;
                    

                    await Task.Run(async () =>
                    {
                        
                        
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
                                       
                                       .OrderByDescending(_ => _.Value)
                                       .Select(_ => new EmotionalEvent
                                       {
                                           Emotion = _.Key,
                                       
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
                var result = await client.PostAsync("https://awesomebot.azurewebsites.net/api/webhooks", content);
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

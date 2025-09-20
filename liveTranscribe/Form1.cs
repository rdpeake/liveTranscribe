using EchoSharp.Abstractions.Audio;
using EchoSharp.Abstractions.SpeechTranscription;
using EchoSharp.NAudio;
using EchoSharp.Onnx.SileroVad;
using EchoSharp.SpeechTranscription;
using EchoSharp.Whisper.net;
using NAudio.CoreAudioApi;
using NAudio.Wasapi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System.Globalization;
using System.Net;
using Whisper.net;
using Whisper.net.Ggml;
using Whisper.net.LibraryLoader;
namespace liveTranscribe
{
    public partial class Form1 : Form
    {
        WaspiLoopbackAudioSource waveloop;
        Task showTranscriptTask;
        Task LoadTask;
        private IRealtimeSpeechTranscriptor realTimeTranscriptor;
        private SileroVadDetectorFactory vadDetectorFactory;
        private WhisperFactory factory;
        private WhisperSpeechTranscriptorFactory speechTranscriptorFactory;
        private EchoSharpRealtimeTranscriptorFactory realTimeFactory;
        CancellationTokenSource token = new CancellationTokenSource();

        public Form1()
        {
            InitializeComponent();
            LoadTask = LoadModels();
        }

        public async Task LoadModels()
        {
            if (!Directory.Exists("models"))
            {
                Directory.CreateDirectory("models");
            }
            var sileroOnnxPath = "models/silero_vad.onnx";
            if (!File.Exists(sileroOnnxPath))
            {
                using (var client = new WebClient())
                {
                    client.DownloadFile(new Uri("https://github.com/sandrohanea/silero-vad/raw/refs/tags/v1/src/silero_vad/data/silero_vad.onnx"), sileroOnnxPath);
                }
            }
            vadDetectorFactory = new SileroVadDetectorFactory(new SileroVadOptions(sileroOnnxPath)
            {
                Threshold = 0f, // The threshold for Silero VAD. The default is 0.5f.
                ThresholdGap = 0f, // The threshold gap for Silero VAD. The default is 0.15f.
            });
            var ggmlModelPath = "models/ggml-Medium.bin";
            if (!File.Exists(ggmlModelPath))
            {
                using var modelStream = await WhisperGgmlDownloader.Default.GetGgmlModelAsync(GgmlType.Medium);
                using var fileWriter = File.OpenWrite(ggmlModelPath);
                await modelStream.CopyToAsync(fileWriter);
            }
            factory = WhisperFactory.FromPath(ggmlModelPath);
            speechTranscriptorFactory = new WhisperSpeechTranscriptorFactory(factory.CreateBuilder().WithTranslate().WithLanguageDetection());

            realTimeFactory = new EchoSharpRealtimeTranscriptorFactory(speechTranscriptorFactory, vadDetectorFactory, echoSharpOptions: new EchoSharpRealtimeOptions()
            {
                ConcatenateSegmentsToPrompt = false // Flag to concatenate segments to prompt when new segment is recognized (for the whole session)
            });

            realTimeTranscriptor = realTimeFactory.Create(new RealtimeSpeechTranscriptorOptions()
            {
                AutodetectLanguageOnce = false, // Flag to detect the language only once or for each segment
                IncludeSpeechRecogizingEvents = true, // Flag to include speech recognizing events (RealtimeSegmentRecognizing)
                RetrieveTokenDetails = true, // Flag to retrieve token details
                LanguageAutoDetect = false, // Flag to auto-detect the language
                Language = new CultureInfo("en-US"), // Language to use for transcription
            });
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await LoadTask;
            if (waveloop == null)
            {


                waveloop = new WaspiLoopbackAudioSource();
                //waveloop = new MicrophoneInputSource();

                async Task ShowTranscriptAsync()
                {
                    await foreach (var transcription in realTimeTranscriptor.TranscribeAsync(waveloop, token.Token))
                    {
                        var eventType = transcription.GetType().Name;
                        this.Invoke(new Action(() => label1.Text = eventType));

                        var textToWrite = transcription switch
                        {
                            RealtimeSegmentRecognized segmentRecognized => $"{segmentRecognized.Segment.StartTime}-{segmentRecognized.Segment.StartTime + segmentRecognized.Segment.Duration}:{segmentRecognized.Segment.Text}",
                            RealtimeSegmentRecognizing segmentRecognizing => $"{segmentRecognizing.Segment.StartTime}-{segmentRecognizing.Segment.StartTime + segmentRecognizing.Segment.Duration}:{segmentRecognizing.Segment.Text}",
                            RealtimeSessionStarted sessionStarted => $"SessionId: {sessionStarted.SessionId}",
                            RealtimeSessionStopped sessionStopped => $"SessionId: {sessionStopped.SessionId}",
                            _ => string.Empty
                        };
                        this.Invoke(new Action(() => label2.Text = textToWrite));
                    }
                    this.Invoke(new Action(() => label2.Text = "Completed"));
                }
                ;
                waveloop.StartRecording();
                showTranscriptTask = ShowTranscriptAsync();

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (waveloop != null)
            {
                MessageBox.Show(waveloop.Duration.ToString());
                waveloop.StopRecording();
                waveloop.Dispose();
                waveloop = null;
                token.Cancel();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            if (waveloop != null)
            {
                waveloop.StopRecording();
                waveloop.Dispose();

                factory.Dispose();
                speechTranscriptorFactory.Dispose();
    }
        }

    }
}

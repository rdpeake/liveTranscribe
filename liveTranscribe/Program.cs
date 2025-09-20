using EchoSharp.Abstractions.SpeechTranscription;
using EchoSharp.Abstractions.VoiceActivityDetection;
using EchoSharp.NAudio;
using EchoSharp.Onnx.SileroVad;
using EchoSharp.SpeechTranscription;
using EchoSharp.Whisper.net;
using System.Globalization;
using Whisper.net;
using Whisper.net.Ggml;

namespace liveTranscribe
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());


        }
    }
}
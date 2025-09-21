using EchoSharp.Audio;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace liveTranscribe
{
    internal class WaspiLoopbackAudioSource : AwaitableWaveFileSource
    {
        WasapiLoopbackCapture waveloop;
        WaveFormat format;
        WaveFormat inFormat;
        private WaveFileWriter? waveFile;

        public byte[] ToPCM16(byte[] buffer, int length, WaveFormat format)
        {
            if (length == 0)
            {
                return new byte[0];
            }

            using var memStream = new MemoryStream(buffer, 0, length);
            using var inputStream = new RawSourceWaveStream(memStream, format);

            var convertedPCM = new SampleToWaveProvider16(
                    new WdlResamplingSampleProvider(
                        new WaveToSampleProvider(inputStream), 16000)
                );

            byte[] convertedBuffer = new byte[length];

            using var stream = new MemoryStream();
            int read;

            while ((read = convertedPCM.Read(convertedBuffer, 0, length)) > 0)
                stream.Write(convertedBuffer, 0, read);

            return stream.ToArray();
        }

        public WaspiLoopbackAudioSource() : base(aggregationStrategy: DefaultChannelAggregationStrategies.SelectChannel(0))
        {
            
            waveloop = new WasapiLoopbackCapture();

            Initialize(new AudioSourceHeader()
            {
                BitsPerSample = 16,
                Channels = 1,
                SampleRate = 16000
            });
            
            
            //format = new WaveFormat(16000, 2);
            inFormat = waveloop.WaveFormat;
            
            waveloop.DataAvailable += Waveloop_DataAvailable;
            waveloop.RecordingStopped += Waveloop_RecordingStopped;
        }

        public void StartRecording()
        {
            waveloop.StartRecording();
            //waveFile = new WaveFileWriter("testStream.wav", format);
        }

        public void StopRecording()
        {
            waveloop.StopRecording();
            //waveFile?.Flush();
            //waveFile?.Close();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                waveloop.DataAvailable -= Waveloop_DataAvailable;
                waveloop.RecordingStopped -= Waveloop_RecordingStopped;
                waveloop.Dispose();
            }
            base.Dispose(disposing);
        }

        private void Waveloop_DataAvailable(object? sender, WaveInEventArgs e)
        {



            var buffer = ToPCM16(e.Buffer, e.BytesRecorded, inFormat);
            //waveFile?.Write(buffer, 0, buffer.Length);
            WriteData(buffer.AsMemory(0, buffer.Length));
        }

        private void Waveloop_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                throw e.Exception;
            }
            Flush();
        }
    }
}

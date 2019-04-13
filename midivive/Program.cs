using NAudio.Midi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Mono.Options;

namespace MidiTest
{
    class Program
    {

        static void Main(string[] args)
        {
            bool show_help = false;
            string inputFile = null;
            float volume = 0.25F;

            OptionSet optionSet = new OptionSet() {
                { "i|input=", "name of the *.mid file",
                   v => inputFile = v },
                { "v|volume=",
                   "playback volume (haptic intensity)",
                    (float v) => volume = v },
                { "h|help",  "show this message and exit",
                   v => show_help = v != null },
            };

            List<string> extra;
            try
            {
                extra = optionSet.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine("Error while parsing arguments: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try starting with '--help' for more information.");
                return;
            }

            if (show_help)
            {
                optionSet.WriteOptionDescriptions(Console.Out);
                return;
            }

            if(inputFile == null)
            {
                Console.WriteLine("Input file not set! Use -i <*.mid>");
                return;
            }
            

            Run(inputFile, volume);
        }

        


        static void Run(string inputFile, float volume)
        {
            if (!OpenVRHelper.InitHMD())
            {
                return;
            }
            
            if (!File.Exists(inputFile))
            {
                Console.WriteLine("{0} doesn't exist!", inputFile);
            }
            
            MidiFile midi = new MidiFile(inputFile, false);


            Player[] players = new Player[2];
            for (int i = 0; i < players.Count(); i++)
            {
                players[i] = new Player(i, false);
            }



            double bpm = 120;

            int divison = midi.DeltaTicksPerQuarterNote;


            int[] trackEventIndex = new int[midi.Events.Count()];
            long currentTick = 0;
            Stopwatch sw = Stopwatch.StartNew();
            double ticksPerQuarter = 1;
            //based on https://github.com/ipatix/serialmidi/blob/master/serialmidi/Program.cs
            //and https://gitlab.com/Pilatomic/SteamControllerSinger/blob/master/main.cpp
            while (true)
            {
                int tracksStopped = 0;

                for (int trackNum = 0; trackNum < midi.Events.Count(); trackNum++)
                {
                    var track = midi.Events[trackNum];
                    if (trackEventIndex[trackNum] >= track.Count())
                        continue;

                    while (currentTick >= track[trackEventIndex[trackNum]].AbsoluteTime)
                    {
                        var ev = track[trackEventIndex[trackNum]];
                        if (ev.CommandCode == MidiCommandCode.NoteOn)
                        {
                            NoteOnEvent note = (NoteOnEvent)ev;
                            if (note.OffEvent == null)
                            {
                                Console.WriteLine("Note {0} doesn't have an off event, skipping.", note.NoteName);
                            }
                            else
                            {
                                //calculate note duration 
                                double duration = note.NoteLength * ticksPerQuarter / 10000;

                                //calculate frequency from the note number
                                //https://pages.mtu.edu/~suits/NoteFreqCalcs.html
                                double frequency = (440F * Math.Pow(2, (note.NoteNumber - 69) / 12F));

                                for (int i = 0; i < players.Count(); i++)
                                {
                                    if (players[i].IsBusy())
                                        continue;
                                    Console.WriteLine("Controller {0}: note {1} ({2:0.##} Hz) for {2:0.##} ms", i, note.NoteName, frequency, duration);
                                    players[i].Play(duration, frequency, volume);
                                    break;
                                }
                            }

                        }
                        if (ev.CommandCode == MidiCommandCode.MetaEvent)
                        {
                            MetaEvent meta = (MetaEvent)ev;
                            if (meta.MetaEventType == MetaEventType.SetTempo)
                            {
                                TempoEvent tempoEvent = (TempoEvent)meta;
                                bpm = tempoEvent.Tempo;
                                Console.WriteLine("New tempo: " + bpm + " BPM");
                                //https://stackoverflow.com/questions/2038313/converting-midi-ticks-to-actual-playback-seconds
                                ticksPerQuarter = (60 * Stopwatch.Frequency) / (bpm * divison);
                            }
                        }
                        trackEventIndex[trackNum]++;
                        if (trackEventIndex[trackNum] >= track.Count - 1)
                        {
                            tracksStopped++;
                            Console.WriteLine("break " + midi.Events.Count());
                            break;
                        }

                    }
                }
                //FIXME: playback noes not stop
                if (tracksStopped == midi.Events.Count())
                    break;

                currentTick++;

                //just skip cycles for now
                while (sw.ElapsedTicks < ticksPerQuarter)
                {
                    ;
                }
                sw.Restart();
            }
            sw.Stop();

            Console.WriteLine("finished");
            OpenVRHelper.Shutdown();
        }

        class Beep
        {
            private readonly float duration;
            private readonly float frequency;

            public Beep(float frequency, float duration)
            {
                this.duration = duration;
                this.frequency = frequency;
            }

            public void Play()
            {
                new Thread(Play0).Start();
            }

            private void Play0()
            {
                SignalGenerator sineWaveProvider = new SignalGenerator
                {
                    Frequency = frequency
                };
                WaveOut waveOut = new WaveOut();
                waveOut.Init(sineWaveProvider);
                waveOut.Play();
                Thread.Sleep((int)duration);
                waveOut.Stop();
            }
        }
        
        class Player
        {
            private long started;
            private double duration;
            private readonly int controller;
            private readonly bool debugMode;

            public Player(int controller, bool debugMode)
            {
                this.controller = controller;
                this.debugMode = debugMode;
            }

            private long GetUnixTime()
            {
                return ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeMilliseconds();
            }

            public bool IsBusy()
            {
                return GetUnixTime() - started < duration;
            }

            public void Play(double durationMS, double frequency, float volume)
            {
                started = GetUnixTime();
                duration = durationMS;
                if (debugMode)
                {
                    new Beep((int)frequency, (int)duration).Play();
                }
                
                OpenVRHelper.PlayNote(controller, (float)durationMS/1000F, (float)frequency, volume);
            }
        }
        
    }

    
}

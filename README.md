# midivive
WIP midi file player for Vive Controllers' haptic actuators.

# Requirements

* Vive Controllers **with the latest firmware** (factory one doesn't allow fine control over frequency of haptic pulses)
* .NET 4.6+
* SteamVR



# Sample usage

`midivive.exe -i file.mid`

# Command line options
| Option                 | Description                                                                                                         |
|------------------------|---------------------------------------------------------------------------------------------------------------------|
| -i, --input=<file.mid> | Path to the *.mid file                                                                                              |
| -v, --volume=0.25      | Playback volume, default is 0.25                                                                                    |
| -t, --tolerance=40     | Time until the end of the note end during which controller can start playing another note, interrupting current one |
| -d, --debug            | Enables debug mode. Sounds will be played into speakers instead of controllers. Can work without SteamVR running    |
| -h, --help             | Shows help text                                                                                                     |

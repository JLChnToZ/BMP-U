# BananaBeats

**!! This branch is a work-in-progress rewrite of the game, not ready for playing !!**
In this branch, currently are experimenting with the new Unity ECS, and migrate to UniRx and UniTask. There are lots of features not re-implemented from the old one to here, including the playable logic and the song select interfece. Stay tuned for updates :)

**Yet-Another Unity-based [Be-Music Script/Source](https://en.wikipedia.org/wiki/Be-Music_Source) (BMS) Player**

## Currently Supported BMS Features

- Channel 10-29 (Normal playable tracks)
- Channel 50-69 (Long hold note tracks)
- BPM / Measure Beat changes
- WAV / OGG Sound Effect / BGM
- Static frame BGAs
- Video file BGAs (Via VideoStreamer / FFMPEG)
- [Bmson format](https://bmson.nekokan.dyndns.info/)

## To-Do

This is the to-do list of this branch:
- [ ] Main Menu
  - [ ] Fancy Title Screen
  - [ ] Song Selection UI - By Folders
  - [ ] Options UI
	- [ ] Language Selection
    - [x] Layout Presets
	- [x] Input Configuration By Layout
	- [ ] Timing Adjustment
	- [ ] Feature Toggles
	- [ ] Speed Adjust
  - [ ] Song Search UI
  - [ ] Score Records
- [ ] Game Scene
  - [ ] BananaBeats Original Mode
  - [x] "Classic" Mode
  - [x] HUD Displaying Song Info, Progress, Score, Combos, Accuracy And Hitted Note Ranks
  - [x] Auto Mode
  - [x] Toggleable Auto Trigger Long Note Ends
  - [x] Toggleable BPM Driven Note Speed And Pausing
  - [ ] Timing Adjustment
  - [x] Keyboard Input
  - [x] Joystick Input (For controllers like IIDX etc.)
  - [ ] Touch Screen Input
  - [ ] Speed Adjustment
  - [ ] Score Result Screen
- [ ] Online ranking: Non-critical feature.
- [ ] Integration of third-party APIs such as BMS search, etc. -- Non-critical feature and this may require to negotiate with third-party service providers.

## How to Play

TBD, should be same as the old ver.

## Copyright Notice / Acknowledgements

TBD, requires update

## Contributing

Feel free to clone, contribute, give suggestions and/or send pull requests!
Currently the whole stuff is lack of graphics and some of the features are missing, it would be great if anyone can help!

## References

- **[BMS command memo](http://hitkey.nekokan.dyndns.info/cmds.htm)**: Even this is a draft memo, it is deep enough to study the whole BMS format.
- **[Bmson Format Specification](https://bmson-spec.readthedocs.io/en/master/doc/)**

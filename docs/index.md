# Refract
**Refract** is a real-time holographic streaming toolkit for [Looking Glass Portrait](https://lookingglassfactory.com/portrait). When combined with [ReGlass][ReGlass], **Refract** allows you to play more than 500 commercial games as holograms on your Portrait.

[![](Images/VideoPreview.png)](https://youtu.be/YKgHC-UgFOY)
[Watch the video](https://youtu.be/YKgHC-UgFOY)

## How Does it Work?
Once installed, [ReGlass][ReGlass] draws your game with color on one side and depth on the other. 

<img src="Images/RGSamp01.png" width=400> &nbsp; &nbsp; <img src="Images/RGSamp02.png" width=400>

This works great for screenshots and video captures, but it doesn't allow you to actually *play* the game on your Portrait. That's where **Refract** comes in.

Refract uses the color and depth information coming from ReGlass to *dynamically generate* a holographic scene with multiple camera angles. The holographic scene is then rendered on the Portrait in real-time.  

## System Requirements
As you can imagine, Refract works best on high-end systems. Refract is essentially running two games at the same time, but I have done what I can to support as many systems as possible. Several settings can be adjusted, and I've included a whole section below on improving performance.

Here are some estimates:

**Nvidia 3080** can run modern games like Cyberpunk at close to 60 FPS without sacrificing hologram quality. Though you'll want to turn off Ray Tracing and lower the resolution.

**Nvidia 1080** can *probably* handle classic games like Portal 2 close to 60 FPS without sacrificing hologram quality.

Going down from there, you'll likely need to turn on **Interpolation** (camera angle skipping) to achieve higher frame rates.       

## Usage
1. Download [ReGlass][ReGlass] and get it fully working with your game. Use the ReGlass [Game Settings][RGGameSettings] page for help with this process.
1. Download the latest [Refract Archive](https://github.com/SolerSoft/Refract/releases/download/v1.1/Refract_1.0.zip) and unzip it.
1. (Optional) create a shortcut to **Refract.exe** on your Start menu or task bar.
1. Launch your game and enable **ReGlass**.
1. Launch **Refract.exe** and Enjoy! 

## Configuration
Refract includes a menu that runs *right on the Portrait!* To bring up the menu, press the Play/Pause button on the side of your portrait.



## Improving Performance
Tips 


## Questions
*Why "Refract"?*

Wikipedia defines [refraction](https://en.wikipedia.org/wiki/Refraction) as "the change in direction of a wave passing from one medium to another". Since **Refract** redirects light from a 2D display to a holographic one, I thought the name was appropriate.

*Why is [ReGlass][ReGlass] associated with [jbienz][jbienz] but Refract associated with [SolerSoft][SolerSoft]?*

I usually release open source software under my personal github ([jbienz][jbienz]), but since **Refract** includes binary executables I wanted to be a little more careful. SolerSoft is a LLC I created years ago exactly for this purpose. Still, note that none of my open source projects include any kind of warranty. Use at your own risk. ;)

## About the Author
Jared Bienz
<img src="Images/JBienz.jpg" width=200>
[<img src="Images/LILogo.png" width=24> jbienz](https://www.linkedin.com/in/jbienz) &nbsp; [<img src="Images/WebLogo.png" width=24> Road to MR](https://www.roadtomr.com) &nbsp; [<img src="Images/WebLogo.png" width=24> Blog](https://jared.bienz.com) &nbsp; [<img src="Images/TwitLogo.png" width=24> @jbienz](https://twitter.com/jbienz)

I'm not always online, but my user name is **eXntrc** on the [Looking Glass Discord](https://discord.com/invite/lookingglassfactory).


<sub>**No Express or Implied Warranty.** This software is provided "as is", with all faults. There are no warranties or guarantees, express or implied.</sub>

[ReGlass]: https://jbienz.github.io/ReGlass "ReGlass Home Page"
[RGGameSettings]: https://jbienz.github.io/ReGlass/GameSettings.html "ReGlass Game Setting"
[jbienz]: https://github.com/jbienz "JBienz on GitHub"
[SolerSoft]: https://github.com/SolerSoft "SolerSoft on GitHub"
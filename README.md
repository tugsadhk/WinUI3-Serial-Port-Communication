# WinUI 3 Serial Port Communication

A serial port communication tool built with WinUI 3 and .NET 8, featuring a clean two-panel interface for sending and receiving data.

[![Microsoft Store](https://img.shields.io/badge/Microsoft%20Store-Download-blue?logo=microsoft)](https://www.microsoft.com/store/productId/9N97V1RZGT1P)

## Screenshots

![Main Window](https://user-images.githubusercontent.com/35468866/142761957-9c3f2fc4-ddcd-4ab6-a87d-30525fbd2813.png)

![Communication Panel](https://user-images.githubusercontent.com/35468866/142762038-f1e115b1-16ac40e8-9ddf-4dfd10efb74d.png)

## Features

**Port Settings**
- Port selection with one-click refresh
- Baud rate: 4800 / 9600 / 19200 / 38400 / 57600 / 115200 / 128000 / 921600
- Data bits: 5 / 6 / 7 / 8
- Stop bits: 1 / 1.5 / 2
- Parity: None / Odd / Even / Mark / Space
- Encoding: UTF-8 / ASCII / Latin-1 (ISO-8859-1)
- DTR & RTS toggles

**Send**
- Line ending options: None / CR / LF / CR+LF
- Send history navigation with ↑ ↓ arrow keys
- TX byte counter

**Receive**
- Auto-scroll toggle
- Timestamp display
- Hex view mode
- RX byte counter
- Save output to file
- Clear button

**Error Log**
- Real-time error tracking with count badge

## Installation

### From Microsoft Store (Recommended)

Get the latest stable build directly from the Store:

[Download on Microsoft Store](https://www.microsoft.com/store/productId/9N97V1RZGT1P)

### Build from Source

**Requirements:**
- Visual Studio 2022
- Windows App SDK extension for VS2022

Install the Windows App SDK extension:
- [VS2022 C#](https://aka.ms/windowsappsdk/stable-vsix-2022-cs)
- [VS2019 C#](https://aka.ms/windowsappsdk/stable-vsix-2019-cs)

More info: [Set up your development environment](https://docs.microsoft.com/en-us/windows/apps/windows-app-sdk/set-up-your-development-environment?tabs=vs-2022)

Open the `.sln` file in Visual Studio, build, and run.

## Tech Stack

- WinUI 3
- .NET 8
- Windows App SDK 1.8
- C#

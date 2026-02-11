# NtpTick

[![NuGet](https://img.shields.io/nuget/v/NtpTick.svg)](https://www.nuget.org/packages/NtpTick)
[![NuGet Downloads](https://img.shields.io/nuget/dt/NtpTick.svg)](https://www.nuget.org/packages/NtpTick)

A simple, lightweight NTP (Network Time Protocol) client library for .NET.

## ✨ Features

- 🕐 Query accurate time from NTP servers
- 🎯 Simple and intuitive API
- 📦 No external dependencies
- ⚙️ Supports .NET 8, .NET 9, and .NET 10
- 🛠️ Access to low-level NTP packet with clean API

## 🚀 Quick Start

```csharp
using NtpTick;

var client = new SimpleNtpClient("time.cincura.net");
var time = await client.GetTime();
Console.WriteLine($"Current time: {time}");
```

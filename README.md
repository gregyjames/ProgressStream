![Nuget](https://img.shields.io/nuget/dt/ProgressStream?style=flat-square)
![Nuget](https://img.shields.io/nuget/v/ProgressStream?style=flat-square)
![GitHub](https://img.shields.io/github/license/gregyjames/ProgressStream?style=flat-square)
[![.NET](https://github.com/gregyjames/ProgressStream/actions/workflows/dotnet.yml/badge.svg)](https://github.com/gregyjames/ProgressStream/actions/workflows/dotnet.yml)

# ProgressStream
Memory Efficient C# Streams that return progress. Ever wish you could track the progress as you write or read from a stream? Well, this is the library for you!

## Sample Usage
    var bytesReadOverall = 0;  
    var readProgress = new Progress<int>(bytesRead =>  
    {  
        Console.WriteLine($"{bytesReadOverall}/10485760 -> {Convert.ToDecimal(bytesReadOverall/10485760.0):P}");  
    });  
    await using var stream = new ProgressStream.ProgressStream(content.ReadAsStreamAsync().Result, readProgress);  
    await using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);  
    while (true)  
    {  
        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);  
        if (bytesRead == 0)  
        {  
            break;  
        }  
        fileStream.Write(buffer,0, bytesRead);  
        bytesReadOverall += bytesRead;  
    }  
      
    Console.WriteLine("Downloaded file: " + fileName);

## License
MIT License

Copyright (c) 2023 Greg James

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

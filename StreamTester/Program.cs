namespace StreamTester
{
    public static class Program
    {
        public static async Task Main()
        {
            var url = "http://speedtest.ftp.otenet.gr/files/test10Mb.db"; // replace with the URL of the file you want to download
            var fileName = "test10Mb.db"; // replace with the name you want to give to the downloaded file
            var buffer = new byte[1024760];
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            using var content = response.Content;
            
            int bytesReadOverall = 0;
            var readProgress = new Progress<int>(bytesRead =>
            {
                Console.WriteLine($"{bytesReadOverall}/10485760 -> {Convert.ToDecimal(bytesReadOverall/10485760.0):P}");
            });
            Console.WriteLine("Starting download with progress...");
            await using var stream = new ProgressStream.ProgressStream(content.ReadAsStreamAsync().Result, readProgress);
            await using var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
            while (true)
            {
                var bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    break;
                }
                fileStream.Write(buffer,0, bytesRead);
                bytesReadOverall += bytesRead;
            }

            Console.WriteLine("Downloaded file: " + fileName);
        }
    }
}
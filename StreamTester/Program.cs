namespace StreamTester
{
    public static class Program
    {
        public static async Task Main()
        {
            var url = "https://link.testfile.org/15MB"; // replace with the URL of the file you want to download
            var fileName = "test10Mb.db"; // replace with the name you want to give to the downloaded file
            var buffer = new byte[1024760];

            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            using var content = response.Content;

            var readProgress = new Progress<ProgressStream.StreamProgress>(progress =>
            {
                var percentText = progress.Percentage is double p ? $"{p:F2}%" : "N/A";
                Console.WriteLine($"{progress.TotalBytesTransferred}/{progress.TotalLength?.ToString() ?? "?"} -> {percentText}");
            });

            Console.WriteLine("Starting download with progress...");

            try
            {
                await using var stream = new ProgressStream.ProgressStream(
                    await content.ReadAsStreamAsync(),
                    readProgress,
                    totalLength: content.Headers.ContentLength);
                await using var fileStream =
                    new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);

                while (true)
                {
                    var bytesRead = await stream.ReadAsync(buffer);

                    if (bytesRead == 0)
                    {
                        break;
                    }

                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
                }
            }
            finally
            {
                Console.WriteLine($"Downloaded file: {fileName}");
            }
        }
    }
}
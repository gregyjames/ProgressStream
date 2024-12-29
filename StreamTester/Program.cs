namespace StreamTester
{
    public static class Program
    {
        
        public static async Task<long> GetFileSizeAsync(string url)
        {
            using var httpClient = new HttpClient();

            try
            {
                // Create a HEAD request
                using var request = new HttpRequestMessage(HttpMethod.Head, url);
            
                // Send the HEAD request
                using var response = await httpClient.SendAsync(
                    request, 
                    HttpCompletionOption.ResponseHeadersRead
                ).ConfigureAwait(false);

                // Ensure success (throw if non-success)
                response.EnsureSuccessStatusCode();

                // Check the Content-Length header
                long? contentLength = response.Content.Headers.ContentLength;
            
                return contentLength ?? -1;
            }
            catch (Exception)
            {
                // If there's an error or if HEAD is not supported,
                // you can choose to fallback to other logic or return -1.
                return -1;
            }
        }
        
        public static double CalculatePercent(int intValue, long longValue)
        {
            if (longValue == 0)
            {
                // Decide how you want to handle division by zero.
                // Here, we'll just return 0 or you could throw an exception.
                return 0;
            }

            // Convert to double to get a fractional result, 
            // then multiply by 100 to express as a percentage.
            double result = (double)intValue / longValue;
            return result;
        }
        
        public static async Task Main()
        {
            var url = "https://link.testfile.org/15MB"; // replace with the URL of the file you want to download
            var fileName = "test10Mb.db"; // replace with the name you want to give to the downloaded file
            var size = GetFileSizeAsync(url).Result;
            var buffer = new byte[1024760];
            using var httpClient = new HttpClient();
            using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            using var content = response.Content;
            
            int bytesReadOverall = 0;
            var readProgress = new Progress<int>(bytesRead =>
            {
                bytesReadOverall += bytesRead;
                Console.WriteLine($"{bytesReadOverall}/{size} -> {CalculatePercent(bytesReadOverall, size):P}");
            });
            Console.WriteLine("Starting download with progress...");
            try
            {
                await using var stream =
                    new ProgressStream.ProgressStream(content.ReadAsStreamAsync().Result, readProgress);
                await using var fileStream =
                    new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
                while (true)
                {
                    var bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break;
                    }
                    fileStream.Write(buffer, 0, bytesRead);
                }
            }
            finally
            {
                Console.WriteLine("Downloaded file: " + fileName);
            }
            
        }
    }
}
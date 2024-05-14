using System;
using Microsoft.Extensions.Logging;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace SeleniumTest
{
    class HomeTesting
    {
        private static readonly ILogger<HomeTesting> _logger;

        static HomeTesting()
        {
            // Konfigurasi logger
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });

            _logger = loggerFactory.CreateLogger<HomeTesting>();
        }

        public static void Main(string[] args)
        {
            TestResult result = RunTest();

            _logger.LogInformation($"Status: {result.Status}");
            _logger.LogInformation($"Path Gambar: {result.ImagePath}");
        }

        public static TestResult RunTest()
        {
            // Mengatur opsi ChromeOptions untuk mode headless dan resolusi layar
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            options.AddArgument("--window-size=1920,1080");

            // Membuat instance dari ChromeDriver dengan opsi yang telah diatur
            using (var driver = new ChromeDriver(options))
            {
                // Mengarahkan browser ke URL yang ingin diuji
                driver.Navigate().GoToUrl("https://lelangagunan.bni.co.id");

                // Mengambil tangkapan layar dan menyimpannya sebagai file PNG
                var screenshot = ((ITakesScreenshot)driver).GetScreenshot();
                string imagePath = "./Files/Testing/LelangAgunan/Home.png";
                screenshot.SaveAsFile(imagePath);

                _logger.LogInformation("Screenshot telah diambil.");

                // Mengembalikan status dan path gambar
                return new TestResult
                {
                    Status = "Success",
                    ImagePath = imagePath
                };
            }
        }
    }

    // Definisi objek untuk menyimpan hasil tes
    public class TestResult
    {
        public string Status { get; set; }
        public string ImagePath { get; set; }
    }
}

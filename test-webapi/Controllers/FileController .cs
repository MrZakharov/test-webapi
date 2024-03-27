using ImageMagick;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Net.Http;
using test_webapi.Extensions;

namespace test_webapi.Controllers {
	[ApiController]
	[Route("api/[controller]")]
	public class FileController : ControllerBase {
		private readonly ILogger<FileController> _logger;
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly IHttpClientFactory _httpClientFactory;

		private readonly string _httpbin = "https://httpbin.org/post";

		public FileController(ILogger<FileController> logger, IWebHostEnvironment webHostEnvironment, IHttpClientFactory httpClientFactory) {
			_logger = logger;
			_webHostEnvironment = webHostEnvironment;
			_httpClientFactory = httpClientFactory;
		}

		[HttpGet]
		public IActionResult Get(string fileName) {
			fileName = Path.Combine(_webHostEnvironment.ContentRootPath, "data", fileName.ClearUnsafePath());
			if (!System.IO.File.Exists(fileName)) {
				_logger.LogError("File {0} not found.", fileName);
				return NotFound();
			}
			try {
				var image = new MagickImage(fileName);
				var fileInfo = MagickFormatInfo.Create(image.Format);

				if (string.IsNullOrEmpty(fileInfo?.MimeType)) {
					throw new Exception($"Unknown file format of file {fileName}");
				}
				_logger.LogInformation("File {0} sent to client.", fileName);
				return File(image.ToByteArray(), fileInfo!.MimeType);
			}
			catch (Exception ex) {
				_logger.LogError(ex.Message);
				throw;
			}
		}

		[HttpDelete]
		public IActionResult Delete(string fileName) {
			fileName = Path.Combine(_webHostEnvironment.ContentRootPath, "data", fileName.ClearUnsafePath());
			if (!System.IO.File.Exists(fileName)) {
				_logger.LogError("File {0} not found.", fileName);
				return NotFound();
			}
			try {
				System.IO.File.Delete(fileName);
			}
			catch (Exception ex) {
				_logger.LogError(ex.Message);
				throw;
			}
			_logger.LogInformation("File {0} deleted.", fileName);
			return Ok();
		}

		[HttpPost]
		public async Task<IActionResult> Upload(IFormFile file) {
			_logger.LogWarning("Achtung! Someone trying to use our dummy service.");
			if (file == null || file.Length == 0) {
				_logger.LogError("Bad File format.");
				return BadRequest("Bad File format.");
			}

			try {
				using var ms = new MemoryStream();
				file.CopyTo(ms);

				ms.Seek(0, SeekOrigin.Begin);
				var image = new MagickImage(ms);

				var fileName = Guid.NewGuid().ToString("N").Remove(8);
				var filePath = Path.Combine(_webHostEnvironment.ContentRootPath, "data", fileName);
				await image.WriteAsync(filePath + ".jpg", MagickFormat.Jpeg);

				var postString = image.ToBase64(MagickFormat.Jpeg);

				var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, _httpbin);
				httpRequestMessage.Content = new StringContent(postString);
				var httpClient = _httpClientFactory.CreateClient();
				var response = await httpClient.SendAsync(httpRequestMessage);
				var stringResponse = await response.Content.ReadAsStringAsync();

				using var writer = new StreamWriter(filePath + ".response");
				await writer.WriteAsync(stringResponse);

				_logger.LogInformation($"File saved as {fileName}.jpg, response saved as {fileName}.response");

				return Ok(fileName + ".jpg");
			}
			catch (BadImageFormatException ex) {
				_logger.LogError(ex, "Error while converting image.");
				return BadRequest("Error while converting image.");
			}
			catch (Exception ex) when (ex is MagickException) {
				_logger.LogError(ex, "Something wrong with image.");
				return BadRequest("Something wrong with image.");
			}
			catch(Exception ex) {
				_logger.LogError(ex, "Something wrong.");
				return BadRequest("Something wrong.");
			}

		}
	}
}

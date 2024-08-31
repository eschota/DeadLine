using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class ScriptsManager
{
    private readonly string _fileDirectory = Paths.ScriptsManagerFolder;

    public ScriptsManager()
    {
        if (!Directory.Exists(_fileDirectory))
        {
            Directory.CreateDirectory(_fileDirectory);
        }
    }

    [HttpGet("api/scripts")]
    public IActionResult GetScripts()
    {
        var files = Directory.GetFiles(_fileDirectory)
                             .Select(f => new { FileName = Path.GetFileName(f) })
                             .ToList();
        return new JsonResult(files);
    }


    [HttpPost("api/scripts")]
    public IActionResult UploadScript(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return new BadRequestObjectResult("No file uploaded.");
        }

        var filePath = Path.Combine(_fileDirectory, file.FileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            file.CopyTo(stream);
        }

        return new OkObjectResult(new { file.FileName });
    }

    [HttpDelete("api/scripts/{fileName}")]
    public IActionResult DeleteScript(string fileName)
    {
        var filePath = Path.Combine(_fileDirectory, fileName);

        if (System.IO.File.Exists(filePath))
        {
            System.IO.File.Delete(filePath);
            return new OkObjectResult(new { fileName });
        }

        return new NotFoundResult();
    }
}

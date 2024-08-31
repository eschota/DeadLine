using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using Newtonsoft.Json;
using System.Reflection;

internal class StableDiffusion
    {
    static string[] models = { "C:\\stable-diffusion-webui\\models\\Stable-diffusion\\BastardV1.safetensors", "C:\\stable-diffusion-webui\\models\\Stable-diffusion\\realisticVisionV60B1_v51HyperVAE.safetensors", "C:\\stable-diffusion-webui\\models\\Stable-diffusion\\toonyou_beta6.safetensors", "C:\\stable-diffusion-webui\\models\\Stable-diffusion\\pornmasterPro_v7.safetensors", "C:\\stable-diffusion-webui\\models\\Stable-diffusion\\uberRealisticPornMerge_urpmv12.safetensors"
    , "C:\\stable-diffusion-webui\\models\\Stable-diffusion\\Deliberate_v2.safetensors"};
    public static async Task StableDiffusionTxtToImage(string prompt, string FilePath,int model)
    {
        string url = "http://127.0.0.1:7860/sdapi/v1/txt2img";

       // string[] models = { "C:\\stable-diffusion-webui\\models\\Stable-diffusion\\realisticVisionV60B1_v51HyperVAE.safetensors", "C:\\stable-diffusion-webui\\models\\Stable-diffusion\\pornmasterPro_v7.safetensors" };
       
       await SetModel(models[model]);
       

        var payload = new
        {
            prompt = prompt,
            steps = 6,
            //negative_prompt = "(deformed iris, deformed pupils, semi-realistic, cgi, 3d, render, sketch, cartoon, drawing,  mutated hands and fingers:1.4), (deformed, distorted, disfigured:1.3), poorly drawn, bad anatomy, wrong anatomy, extra limb, missing limb, floating limbs, disconnected limbs, mutation, mutated, ugly, disgusting, amputation",
            sampler_name = "DPM++ SDE",
            cfg_scale = 1.2,
            width = 512,
            height = 512,
           // seed = 9999,
            model = models[model]
        };

        var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
        try {
            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                    string base64Image = responseObject.images[0];

                    byte[] imageBytes = Convert.FromBase64String(base64Image);
                    await File.WriteAllBytesAsync(FilePath, imageBytes);

                    Logger.AddLog("Image saved as output_image.png");
                }
                else
                {
                    await SDAdapter.RestartStableDiffusion();
                    Logger.AddLog($"Failed to generate image. Status code: {response.StatusCode}");
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    Logger.AddLog($"Error details: {errorDetails}");
                }
            }
        }catch (Exception ex) { Logger.AddLog($"Error details: " + ex.Message); }
    }
    public static async Task StableDiffusionImgToImg(string prompt, string inputImagePath, string outputPath, int _model)
    {
        try
        {
            prompt = prompt.Substring(0, prompt.IndexOf('<') - 1);
            string url = "http://127.0.0.1:7860/sdapi/v1/txt2img";
            await SetModel(models[1]);



            // Прочитайте исходное изображение и конвертируйте его в base64
            byte[] imageBytes = File.ReadAllBytes(inputImagePath);
            string base64Image = Convert.ToBase64String(imageBytes);

            var payload = new
            {
                prompt = prompt,
                seed = 9999,
                negative_prompt = "(deformed iris, deformed pupils, semi-realistic, cgi, 3d, render, sketch, cartoon, drawing,  mutated hands and fingers:1.4), (deformed, distorted, disfigured:1.3), poorly drawn, bad anatomy, wrong anatomy, extra limb, missing limb, floating limbs, disconnected limbs, mutation, mutated, ugly, disgusting, amputation",
                //init_image = base64Image,    // Передаем исходное изображение
                // strength = 1.0,             // Степень влияния оригинального изображения (0.0 - полностью новое изображение, 1.0 - минимальные изменения)
                steps = 6,
                sampler_name = "DPM++ SDE",
                cfg_scale = 1.0,
                width = 512,
                height = 512,
                model = models[1],
                alwayson_scripts = new
                {
                    //animatediff = new
                    //{
                    //    args = new[]
                    //    {
                    //        new { enable = true,video_length = 24, model = @"c:\stable-diffusion-webui\extensions\sd-webui-animatediff\model\animatediffMotion_v15V2.ckpt" }//v3.bin" }//,format ="MP4" }//,fps =8,   }

                    //    }

                    //},


                    controlnet = new
                    {
                        args = new[]
                        {
                        new
                        {
                            enabled = true,
                            image = base64Image,
                            module ="ip-adapter-auto",
                            model = "ip-adapter-plus-face_sd15"

                        }
                    }
                    },
                    reactor = new
                    {
                        args = new[]
                        {
                        new
                        {
                            source_image = base64Image,
                        }
                    }
                    }
                }

            };

            var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            using (HttpClient client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(10);
                HttpResponseMessage response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = await response.Content.ReadAsStringAsync();
                    var responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                    string base64OutputImage = responseObject.images[0];

                    byte[] outputImageBytes = Convert.FromBase64String(base64OutputImage);
                    await File.WriteAllBytesAsync(outputPath, outputImageBytes);

                    Logger.AddLog("Image successfully transformed and saved.");
                }
                else
                {
                    await SDAdapter.RestartStableDiffusion();
                    Logger.AddLog($"Failed to transform image. Status code: {response.StatusCode}");
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    Logger.AddLog($"Error details: {errorDetails}");
                }
            }
        }
        catch (Exception ex)
        {
            Logger.AddLog(ex.Message);
        };
    }
    
    public static async Task StableDiffusionGif(string prompt, string inputImagePath, string outputPath, int _model)
    {
        await SetModel(models[1]);
        string url = "http://127.0.0.1:7860/sdapi/v1/txt2img";

        var payload = new
        {
            prompt = prompt+ ", hyper realistic, 8k, cinematic lighting",
            negative_prompt = "(deformed iris, deformed pupils, semi-realistic, cgi, 3d, render, sketch, cartoon, drawing,  mutated hands and fingers:1.4), (deformed, distorted, disfigured:1.3), poorly drawn, bad anatomy, wrong anatomy, extra limb, missing limb, floating limbs, disconnected limbs, mutation, mutated, ugly, disgusting, amputation",
            //init_image = base64Image,    // Передаем исходное изображение
            //strength = 1.0,             // Степень влияния оригинального изображения (0.0 - полностью новое изображение, 1.0 - минимальные изменения)
            steps = 4,
            sampler_name = "DPM++ SDE",
            cfg_scale = 1.5,
            width = 512,
            height = 512,
            model = models[1] ,
            seed = 9999,
            //alwayson_scripts":{"AnimateDiff": {"args": [true,0,24,6,"mm_sd_v15.ckpt"]}



            alwayson_scripts = new
            {
                animatediff = new
                {
                    args = new[]
                    {
                        new { enable = true,video_length = 30,fps =10, model = @"c:\stable-diffusion-webui\extensions\sd-webui-animatediff\model\v3.bin" }//,format ="MP4" }//,fps =8,   }

                    }
                    
                }
            }

        };

        var jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        using (HttpClient client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromMinutes(10);
            HttpResponseMessage response = await client.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var responseObject = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(jsonResponse);
                string base64OutputImage = responseObject.images[0];

                byte[] outputImageBytes = Convert.FromBase64String(base64OutputImage);
                await File.WriteAllBytesAsync(outputPath, outputImageBytes);

                Logger.AddLog("Image successfully transformed and saved.");
            }
            else
            {
                await SDAdapter.RestartStableDiffusion();
                Logger.AddLog($"Failed to transform image. Status code: {response.StatusCode}");
                string errorDetails = await response.Content.ReadAsStringAsync();
                Logger.AddLog($"Error details: {errorDetails}");
            }
        }
    }
    public static async Task SetModel(string modelPath)
    {
       
        HttpClient client = new HttpClient();
        string urlGetOptions = "http://127.0.0.1:7860/sdapi/v1/options";
        string urlSetOptions = "http://127.0.0.1:7860/sdapi/v1/options";

        try
        {
            // Get current options
            HttpResponseMessage responseGet = await client.GetAsync(urlGetOptions);
            if (!responseGet.IsSuccessStatusCode)
            {
                Logger.AddLog($"Failed to get options. Status code: {responseGet.StatusCode}");
                await SDAdapter.RestartStableDiffusion();
                return;
            }

            string jsonResponse = await responseGet.Content.ReadAsStringAsync();
            var options = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

            // Set the model checkpoint
            options.sd_model_checkpoint = Path.GetFileNameWithoutExtension(modelPath);

            var jsonPayload = JsonConvert.SerializeObject(options);
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            // Post updated options
            HttpResponseMessage responsePost = await client.PostAsync(urlSetOptions, content);
            if (responsePost.IsSuccessStatusCode)
            {

                return;
            }
            else
            {
                await SDAdapter.RestartStableDiffusion();
                Logger.AddLog($"Failed to set model. Status code: {responsePost.StatusCode}");
                string errorDetails = await responsePost.Content.ReadAsStringAsync();
                Logger.AddLog($"Error details: {errorDetails}");
            }
        }
        catch (Exception ex)
        {
            await SDAdapter.RestartStableDiffusion();
            Logger.AddLog($"Exception while setting model: {ex.Message}");
        } 


    }
    
} 

using System.Diagnostics;

namespace ConsoleApp1.config;

using System;
using System.IO;

public class AppConfig


{

    
    // 基础配置
    public static string DesktopPath => Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    public static string BotBaseDirectory => Path.Combine(DesktopPath, "机器人");
    
    // 文件路径配置
    public static string ConfigFilePath => Path.Combine(BotBaseDirectory, "bot.json");
    public static string IdiomsFilePath => Path.Combine(BotBaseDirectory, "idiom.json");
    public static string SignFilePath => Path.Combine(BotBaseDirectory, "sign.json");
    public static string PictureDirectory => Path.Combine(BotBaseDirectory, "picture");
    
    // Stable Diffusion API 配置
    public static class StableDiffusion
    {
        public static string ApiUrl => "http://127.0.0.1:7861/sdapi/v1/txt2img";
        
        public static object DefaultRequestBody => new
        {
            enable_hr = false,
            denoising_strength = 0,
            hr_scale = 2,
            hr_second_pass_steps = 0,
            hr_resize_x = 0,
            hr_resize_y = 0,
            hr_prompt = "",
            hr_negative_prompt = "",
            prompt = "",
            seed = -1,
            subseed = -1,
            subseed_strength = 0,
            seed_resize_from_h = -1,
            seed_resize_from_w = -1,
            batch_size = 1,
            n_iter = 1,
            steps = 50,
            cfg_scale = 7,
            width = 512,
            height = 512,
            restore_faces = false,
            tiling = false,
            do_not_save_samples = false,
            do_not_save_grid = false,
            eta = 0,
            s_min_uncond = 0,
            s_churn = 0,
            s_tmax = 0,
            s_tmin = 0,
            s_noise = 1,
            override_settings = new { },
            override_settings_restore_afterwards = true,
            script_args = Array.Empty<string>(),
            sampler_index = "Euler",
            send_images = true,
            save_images = false,
            alwayson_scripts = new { }
        };
    }
    
    // 确保目录存在
    public static void InitializeDirectories()
    {
        Console.WriteLine(DesktopPath + " " + BotBaseDirectory);
        Directory.CreateDirectory(BotBaseDirectory);
        Directory.CreateDirectory(PictureDirectory);
    }
}
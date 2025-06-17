namespace ConsoleApp1.utils;

public class Debug
{
    public static void PrintTypeAndValue(object obj, string name = null)
    {
        string label = string.IsNullOrEmpty(name) ? "变量" : name;
        if (obj == null)
        {
            Console.WriteLine($"{label} 类型: null, 值: null");
        }
        else
        {
            Console.WriteLine($"{label} 类型: {obj.GetType()}, 值: {obj}");
        }
    }

}
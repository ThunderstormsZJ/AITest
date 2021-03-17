using UnityEngine;

public static class AILog
{
    public static void Bule(string message, params object[] args)
    {
        message = "<color=darkblue>" + message + " </color>";
        Log(message, args);
    }

    public static void Green(string message, params object[] args)
    {
        message = "<color=#079D77>" + message + " </color>";
        Log(message, args);
    }

    public static void Red(string message, params object[] args)
    {
        message = "<color=red>" + message + " </color>";
        Log(message, args);
    }

    public static void Error(string message, params object[] args)
    {
        Debug.LogErrorFormat(message, args);
    }

    public static void Log(string message, params object[] args)
    {
        if (args.Length > 0)
        {
            Debug.LogFormat(message, args);
        }
        else
        {
            Debug.Log(message);
        }
    }
}

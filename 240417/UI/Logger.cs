using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class Logger : MonoBehaviour
{
    const int MaxLineCount = 20;

    StringBuilder sb;

    Queue<string> logLines = new Queue<string>(MaxLineCount + 1);
    
    TextMeshProUGUI log;

    private void Awake()
    {
        log = GetComponent<TextMeshProUGUI>();

        sb = new StringBuilder(MaxLineCount + 1);
    }

    public void Log(string message)
    {
        logLines.Enqueue(message);
        if (logLines.Count > MaxLineCount)
        {
            logLines.Dequeue();
        }

        sb.Clear();
        foreach (string line in logLines)
        {
            sb.AppendLine(line);
        }

        log.text = sb.ToString();
    }
}

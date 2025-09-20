using System;
using UnityEngine;

public static class DialogUtil
{
    /// �������� ������������������ ������ �� ���� DialogPanel.
    public static void ShowLines(DialogPanel panel, string title, string[] lines, Action onClose = null, string subtitle = null)
    {
        if (panel == null || lines == null || lines.Length == 0)
        {
            onClose?.Invoke();
            return;
        }

        int i = 0;

        void Close()
        {
            panel.Show(false);
            onClose?.Invoke();
        }

        void Next()
        {
            bool last = (i >= lines.Length - 1);
            string body = lines[i];

            panel.SetBody(
                title, body,
                last ? "�������" : "�����",
                last ? (Action)Close : () => { i++; Next(); },
                last ? null : "�������",
                last ? null : (Action)Close,
                subtitle
            );
            panel.Show(true);
        }

        Next();
    }
}
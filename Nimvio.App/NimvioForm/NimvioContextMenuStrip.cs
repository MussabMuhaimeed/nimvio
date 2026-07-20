namespace Nimvio;

internal sealed class NimvioContextMenuStrip : ContextMenuStrip
{
    protected override bool ProcessCmdKey(ref Message message, Keys keyData)
    {
        if (keyData == Keys.Escape)
        {
            Close(ToolStripDropDownCloseReason.Keyboard);
            return true;
        }
        return base.ProcessCmdKey(ref message, keyData);
    }
}

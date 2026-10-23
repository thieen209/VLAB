namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        void ShowWhiteboard()
        {
            // The existing modal pauses the experiment and restores its run intent on close.
            var box=Modal(L("Bảng phác thảo","Sketch board"),1100,800);
            VlabWhiteboard.Render(ui,box,Session?.spec.id??selected?.id??"P01",Vi,CloseModal);
        }
    }
}

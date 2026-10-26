namespace Vlab.Product
{
    public sealed partial class VlabApp
    {
        string GuidanceExplanation(GuideTask task)=>Guide()?.Explanation(task,Vi)??"";
        string GuidanceNext(GuideTask task)=>Guide()?.Next(Apparatus,task,Vi,guidePage)??"";
    }
}

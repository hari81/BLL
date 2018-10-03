using DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace BLL.Core.Domain
{
    public class UCModel : UCDomain
    {
        public UCModel(IUndercarriageContext context) : base(context)
        {

        }

        public static string modelImageUnavailable = "iVBORw0KGgoAAAANSUhEUgAAAHgAAABOCAIAAABkJUU1AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAAAZdEVYdFNvZnR3YXJlAHBhaW50Lm5ldCA0LjAuMTZEaa/1AAAFN0lEQVR4Xu2c2VYiMRRF/f//sx1abWdEcBabx95w0rWKqlRRQyYg+4FlgKWyc+omuaBHk8nk4+NjuVz+zfjh/f396enpiK9eXl6m0+lisdADGVegFLGvr6/keCVad2Ed9xpmxoNfLP/8/GhoRIu3tzce+/7+NuPMIL6+vtBIQTbjNRuiQWnHuBln+kB+qcOz2awIckFVtGA20M3MmHGmA5+fn5Rfbs14E7toYE6YGQpNfXIyFVA0X9PiqlG0YH5ytNtRkLcq2iIaukzXYYIQLnqKchcz20WL9gJ0gHQMckFX0cC8NS2pB0WvIBf0EC20STzYaHOmG7Zo9RYtKNmHFm1e7PPzM0E2454MFA3MKkWqcv7ZVxTkMWfm4aIFM8w873G0XR2Vx4oG5plo72VDqtIYGoMD0YI5J9p702slPSh2mB5noqFov5rxzuKjHroULQZvgFLA3wrvXjQUO6EdWiT5Vb0u7F5Ei76H1IgEaDB4FA2kg3NNyg2pYL+hX9GCpKRZtUO+vxFCNJCXVad1Pjfj2CjIIVeRQKKFou21FHYhyr4oqGhQlMCMw8JP13bIjAMSWrTQKh+4ITW+MTSGOKKF9q0BTu0pHFljiobuvbFlA+bhZvSpoGCLXhORRQs1pAoXRmFJIvNxdXX1u8bFxUXLmqbG0PgOpxOSEA1YxiZyMf7nP0URp6D/auD29lbPKcN383qeHkAqogWiHx4ejo+PMXh6eqouJRNwdnYmrXVOTk4qoUYuu5rU3vpJTjQxnEwmKC7CyNBIXYNZMIM1d3d3PNmKvkMKpCW6AEe4RjGlWVq5pUqwcyC/QOW9v78n9XLNF3V4frnQxyUh0cjleqccU52pFeXYsu5VSoEMYpyZME+qcX19nUVbYIdgDG2CZYTyBG6p4JeXl2w2KBdSz/Q0uc6i7ZBlLYMVdNDgtrIkEnkKOg9xzqxUbZFF27GKJr88RJatGw/8Ktc3NzfmrhJZtB2raO5E1uPjoxnXQDFPYNdsxiWyaDtW0aobLSve+fk5TyDXZlwii7bTIpoCYsY1JJoybcYlsmg7VtFsRZDFltmMa7AXzKWjH1bRFA0eIrAcQMxdm6hnxNnEjEtk0XasotlXSOVsNqvv4VgkUZm3d/2wigYOLHpbhBWPPQZFGUi6yjcHlqYKnkXbaRINuFYnrwL765Z1Mou20yIaKA6IY9GjUABx5hTeVLhFFm2nXXQBxq0VuU4WXYWELhYLlju9O+UKNoVZtIGlDL9eP8HEEprCJ6RiikYB5YI4m7E3+BH8IPVaYxFHNEF+DvuJIV06YMbBiSCajVqsfCnaAa6hOkFFs+IRZB00IkLJJtpk3IyDEE40J+lpMv8iS3+ypzcNwhBCNHLT+cRQGa4trrAwc+9dNC8Gy4Gv0+4EC4FH0bo8rT2K1FBZ8/qJXi+iyW9qH33biu+F2r1o9m2xtlDj0RnKR7Rdiia/q78H2vH/vsQvz+aPK9Ltq3AmmghT5nY0yHWINi/H4anKgWhFYNeDXIeq7TDaY0UHawzFQi9wfLSHi2aeWaYJshnvL8Ula8aDGCja1TzvEFy1Y67d3qIV5JAdzqQg18MaUv1Eq8Pp9QSVPjoo9D3xdhWNXLY70Tuc6aAeTveGVCfRagU42eXsE1gm2h0bUltEK8g70RiKhVK4NdqNoskvK96wwn9oYBnX7dG2i97pxlAsuO7R3bRTqIomv4N3MBmi3dRr3RA9ck+eEdaGlBGtIEMOshOIthpSZizROcieKDcqjsa3SzItUCFWhufzf5LmIP5DdRB8AAAAAElFTkSuQmCC";

        private byte[] getModelImageUnavailable()
        {
            return Convert.FromBase64String(modelImageUnavailable);
        }

        public byte[] getModelImage(int modelId)
        {
            if (modelId <= 0)
                return getModelImageUnavailable();
            var model = _domainContext.MODELs.Find(modelId);
            if (model == null || model.ModelImage == null)
                return getModelImageUnavailable();
            return model.ModelImage;
        }

        public byte[] getMeasurementPonitImage(int measurementPointId)
        {
            if (measurementPointId <= 0)
                return getModelImageUnavailable();
            var measurementPoints = _domainContext.COMPART_MEASUREMENT_POINT.Find(measurementPointId);
            if (measurementPoints == null  || measurementPoints.Image ==null)
                return getModelImageUnavailable();
            return measurementPoints.Image;
        }

        public async Task<byte[]> getModelImageAsync(int modelId)
        {
            if (modelId <= 0)
                return getModelImageUnavailable();
            var model = await _domainContext.MODELs.FindAsync(modelId);
            if (model == null || model.ModelImage == null)
                return getModelImageUnavailable();
            return model.ModelImage;
        }
        public IQueryable<DAL.MODEL> getInventorySystemModels(int JobSiteId, int MakeId) {
            var makeModels = _domainContext.LU_MMTA.Where(m => m.make_auto == MakeId).Select(m => m.model_auto);
            return  _domainContext.LU_Module_Sub.Where(m => m.crsf_auto == JobSiteId && makeModels.Any(k=> k == m.model_auto) && (m.equipmentid_auto == null || m.equipmentid_auto == 0)).Select(m => m.Model);
        }
    }
}
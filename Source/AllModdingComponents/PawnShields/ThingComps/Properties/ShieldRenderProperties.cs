using UnityEngine;
using Verse;

namespace PawnShields
{
    /// <summary>
    /// Extended properties class for grouping it rendering properties into one area.
    /// </summary>
    public class ShieldRenderProperties
    {
        /// <summary>
        /// North offset.
        /// </summary>
        public Vector3 northOffset = new Vector3(-0.3f, -0.017f, -0.3f);

        /// <summary>
        /// South offset.
        /// </summary>
        public Vector3 southOffset = new Vector3(0.3f, 0.033f, -0.3f);

        /// <summary>
        /// West offset.
        /// </summary>
        public Vector3 westOffset = new Vector3(-0.3f, 0.053f, -0.3f);

        /// <summary>
        /// East offset.
        /// </summary>
        public Vector3 eastOffset = new Vector3(0.3f, -0.017f, -0.3f);

        /// <summary>
        /// If true the texture rotation will be flipped when the rotation is North or South.
        /// </summary>
        public bool flipRotation = true;

        /// <summary>
        /// If true the shield will still be rendered even though no fighting is going on.
        /// </summary>
        public bool renderWhenPeaceful = false;

        /// <summary>
        /// Returns the appropiate offset in 3D.
        /// </summary>
        /// <param name="rot">Rotation to give for.</param>
        /// <returns>Appropiate offset.</returns>
        public Vector3 Rot4ToVector3(Rot4 rot)
        {
            if (rot == Rot4.North)
                return northOffset;
            if (rot == Rot4.South)
                return southOffset;
            if (rot == Rot4.West)
                return westOffset;
            if (rot == Rot4.East)
                return eastOffset;

            //Default
            return new Vector3(0f, 0f, 0f);
        }
    }
}

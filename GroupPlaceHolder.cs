using UnityEngine;

namespace KerbalColonies
{
    /// <summary>
    /// A placeholder class for the buildingGroups
    /// This is important for upgrading the groups
    /// </summary>
    internal class GroupPlaceHolder
    {
        internal string GroupName;
        internal Vector3 Position;
        internal Vector3 Orientation;
        internal float Heading = 0f;

        internal GroupPlaceHolder(string groupName, Vector3 position, Vector3 orientation, float heading)
        {
            GroupName = groupName;
            Position = position;
            Orientation = orientation;
            Heading = heading;
        }
    }
}

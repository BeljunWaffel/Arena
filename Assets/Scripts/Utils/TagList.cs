using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Utils
{
    public enum Tag
    {
        Player,
        Projectile
    }

    public class TagList : MonoBehaviour
    {

        public List<Tag> Tags = new List<Tag>();

        public bool ContainsTag(Tag tag)
        {
            return Tags.Contains(tag);
        }

        public static bool ContainsTag(GameObject gameObject, Tag tag)
        {
            TagList tags = gameObject.GetComponent<TagList>();
            return tags != null && tags.ContainsTag(tag);
        }
    }
}
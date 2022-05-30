using UnityEngine;

namespace PG
{
    public class FoldoutAttribute :PropertyAttribute
    {
        public string Name;
        public bool Foldout;

        public FoldoutAttribute (string name, bool foldout = false)
        {
            this.Foldout = foldout;
            this.Name = name;
        }
    }
}
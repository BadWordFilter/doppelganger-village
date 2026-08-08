using System;
using System.Collections.Generic;
using UnityEngine;

namespace DoppelgangerVillage.Data
{
    /// <summary>
    /// Assets/Data/dialogue.json(TextAsset 참조)을 파싱해 동물별로 인덱싱하는 로더.
    /// WebGL 호환을 위해 파일 IO 대신 TextAsset 직렬화 참조를 쓴다.
    /// </summary>
    public class DialogueDatabase
    {
        [Serializable]
        private class Table
        {
            public List<DialogueEntry> entries;
        }

        private readonly Dictionary<string, List<DialogueEntry>> _byAnimal = new();
        public IReadOnlyList<DialogueEntry> All { get; private set; }

        public static DialogueDatabase Load(TextAsset json)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json), "dialogue.json TextAsset이 연결되지 않았습니다.");

            var table = JsonUtility.FromJson<Table>(json.text);
            if (table?.entries == null || table.entries.Count == 0)
                throw new InvalidOperationException("dialogue.json 파싱 실패 또는 항목 없음.");

            var db = new DialogueDatabase { All = table.entries };
            foreach (var e in table.entries)
            {
                if (!db._byAnimal.TryGetValue(e.animal, out var list))
                    db._byAnimal[e.animal] = list = new List<DialogueEntry>();
                list.Add(e);
            }
            return db;
        }

        /// <summary>해당 동물의 전체 대화 목록. 없는 동물이면 빈 리스트.</summary>
        public IReadOnlyList<DialogueEntry> ForAnimal(string animal) =>
            _byAnimal.TryGetValue(animal, out var list) ? list : Array.Empty<DialogueEntry>();

        public IEnumerable<string> Animals => _byAnimal.Keys;
    }
}

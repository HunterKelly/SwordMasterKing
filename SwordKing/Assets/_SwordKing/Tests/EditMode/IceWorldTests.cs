using NUnit.Framework;
using UnityEngine;

namespace SwordKing.Tests
{
    public sealed class IceWorldTests
    {
        [Test]
        public void SeededPlacementIsStableAndClearOfCoverAndArrival()
        {
            var definition=ScriptableObject.CreateInstance<LevelDefinition>();
            try
            {
                definition.arrival=new Vector3(0,.15f,-22);
                definition.randomEnemyCount=24;
                for(int seed=0;seed<100;seed++)
                {
                    definition.placementSeed=seed;
                    var first=IceWorldBuilder.Encounters(definition);
                    var second=IceWorldBuilder.Encounters(definition);
                    Assert.That(first.Length,Is.EqualTo(25));
                    for(int i=0;i<24;i++)
                    {
                        Assert.That(first[i].id,Is.EqualTo(i));
                        Assert.That(first[i].position,Is.EqualTo(second[i].position));
                        Assert.That(IceWorldBuilder.InsideCover(first[i].position),Is.False);
                        Assert.That(Vector3.Distance(first[i].position,definition.arrival),Is.GreaterThanOrEqualTo(8));
                        for(int j=0;j<i;j++) Assert.That(Vector3.Distance(first[i].position,first[j].position),Is.GreaterThanOrEqualTo(3));
                    }
                    Assert.That(first[24].boss,Is.True);
                    Assert.That(first[24].id,Is.EqualTo(24));
                }
            }
            finally { Object.DestroyImmediate(definition); }
        }
    }
}

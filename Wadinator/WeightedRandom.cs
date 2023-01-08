namespace Wadinator; 

/// <summary>
/// Performs a weighted randomization on a set of objects. Objects that are given more weight
/// will have a higher chance of being picked.
/// </summary>
public class WeightedRandom<T> {
    /// <summary>
    /// Represents a candidate. 
    /// </summary>
    private struct Candidate {
        /// <summary>
        /// The object to return.
        /// </summary>
        public T Value;
        
        /// <summary>
        /// The weight of this object.
        /// </summary>
        public int Weight;
    }
    
    private List<Candidate> _candidates = new();
    private Random _rng = new();

    /// <summary>
    /// Adds a candidates to the list of potential selections.
    /// </summary>
    /// <param name="candidate">The candidate to add.</param>
    /// <param name="weight">The weight of the candidate.</param>
    public void Add(T candidate, int weight) {
        _candidates.Add(new Candidate { Value = candidate, Weight = weight });
    }
    
    /// <summary>
    /// Randomly picks a candidate.
    /// </summary>
    /// <returns>The selected candidate.</returns>
    public T? Select() {
        var totalWeight = _candidates.Sum(x => x.Weight);
        
        // Pick a value.
        var value = _rng.Next(totalWeight - 1);
        
        // Shuffle the candidates.
        var finalList = ShuffleList(_candidates);

        // Iterate through the candidates and figure out which value corresponds to which entry.
        var weightOffset = 0;
        foreach(var candidate in finalList) {
            if(value > (weightOffset + candidate.Weight)) {
                weightOffset += candidate.Weight;
                continue;
            }

            return candidate.Value;
        }

        return default;
    }

    /// <summary>
    /// Randomizes a list using the Fisher-Yates shuffle method.
    /// </summary>
    /// <param name="list">The list to shuffle.</param>
    /// <returns>The shuffled list.</returns>
    private List<TList> ShuffleList<TList>(List<TList> list) {
        var n = list.Count;
        while(n > 1) {
            n--;
            var k = _rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }

        return list;
    }
}

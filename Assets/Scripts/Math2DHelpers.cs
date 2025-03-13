using UnityEngine;

public static class Math2DHelpers
{
    public static Vector2 GetRandomUnitVectorWithinAngle(Vector2 forward, float angle)
    {
        // Convert half the angle to radians
        float halfAngle = angle / 2f * Mathf.Deg2Rad;

        // Generate a random angle within the allowed range
        float randomAngle = Random.Range(-halfAngle, halfAngle);

        // Compute the rotated vector
        float cos = Mathf.Cos(randomAngle);
        float sin = Mathf.Sin(randomAngle);

        // Apply 2D rotation matrix
        Vector2 rotatedVector = new Vector2(
            forward.x * cos - forward.y * sin,
            forward.x * sin + forward.y * cos
        );

        return rotatedVector.normalized; // Ensure it remains a unit vector
    }

    public static int GetBiasedRandomNumber()
    {
        int[] numbers = { 2, 3, 4, 5, 6, 7, 8, 9 };
        float[] weights = { 0.05f, 0.10f, 0.15f, 0.20f, 0.20f, 0.15f, 0.10f, 0.05f }; // Higher chance for 5 and 6

        float totalWeight = 0;
        foreach (float weight in weights) totalWeight += weight;

        float randomValue = Random.Range(0f, totalWeight);
        float cumulative = 0;

        for (int i = 0; i < numbers.Length; i++)
        {
            cumulative += weights[i];
            if (randomValue <= cumulative)
                return numbers[i];
        }

        return 5;
    }
}

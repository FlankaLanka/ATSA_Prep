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

    public static Vector2 GetRandomUnitVector2D()
    {
        float angle = Random.Range(0f, Mathf.PI * 2);
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
    }

    public static float NormalizeValue(float value, float min, float max)
    {
        return (value - min) / (max - min);
    }

    public static float SignedDistanceFromPointToLine(Vector2 point, Vector2 linePoint, Vector2 lineDirection)
    {
        // Normalize the direction vector
        lineDirection.Normalize();

        // Find the vector from the line point to the given point
        Vector2 pointToLine = point - linePoint;

        // Compute the perpendicular vector to the line direction
        Vector2 perpendicular = new Vector2(-lineDirection.y, lineDirection.x);

        // Signed distance (dot product projects onto the perpendicular vector)
        float signedDistance = Vector2.Dot(pointToLine, perpendicular);

        return signedDistance;
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

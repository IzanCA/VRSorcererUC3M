using TMPro;
using UnityEngine;

public class GoatGenerator : MonoBehaviour
{
    [SerializeField] private string[] foods = { "Hamburguer", "HotDog" };
    private int randomIndex;
    private bool waitingForFood = false;
    [SerializeField] private TextMeshProUGUI goatText;

    void Start()
    {
        GenerateFood();
    }

    private void GenerateFood()
    {
        randomIndex = Random.Range(0, foods.Length);
        waitingForFood = true;
        Debug.Log("Goat wants: " + foods[randomIndex]);
        
        goatText.text = "La cabra quiere: " + foods[randomIndex];
    }

    private bool MatchesFood(string objectName, string foodName)
    {
        // Limpia el nombre quitando "(Clone)", "(2)", etc.
        string cleanName = objectName
            .Replace("(Clone)", "")
            .Replace("(clone)", "")
            .Trim();

        // Quita cualquier cosa entre paréntesis al final
        int parenIndex = cleanName.LastIndexOf('(');
        if (parenIndex > 0)
            cleanName = cleanName.Substring(0, parenIndex).Trim();

        return cleanName == foodName;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!waitingForFood) return;

        string objName = other.gameObject.name;

        bool isFood = MatchesFood(objName, "Hamburguer") || MatchesFood(objName, "HotDog");
        if (!isFood) return;

        bool correctFood = MatchesFood(objName, foods[randomIndex]);

        Destroy(other.gameObject);
        waitingForFood = false;

        if (correctFood)
        {
            Debug.Log("Correcto");
            goatText.text = "Correcto!";
        }

        else
        {
            Debug.Log("Incorrecto");
            goatText.text = "Incorrecto!";
        }
            
        Invoke("GenerateFood" , 1f);
    }
}
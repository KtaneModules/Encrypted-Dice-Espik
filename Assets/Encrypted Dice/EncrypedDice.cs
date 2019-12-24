using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class EncrypedDice : MonoBehaviour {
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable RollButton;
    public Renderer[] LEDS;

    public Renderer[] RollDice;
    public Renderer[] SubmitDice;
    public KMSelectable[] SelectDice;

    public Material[] DiceMaterials;
    public Material[] LEDMaterials;

    // Logging info
    private static int moduleIdCounter = 1;
    private int moduleId;

    // Solving info
    private int[] rolledValues = new int[3];
    private int[] submittedValues = new int[2];

    private int a, b, c, x, y;

    private int stagesCompleted = 0;
    private bool solved = false;
    private bool canRoll = false;

    private int[] numberIndexes = new int[2];
    private int[] numbers = { 1, 2, 3, 4, 5, 6 };

    // Runs when the bomb loads
    private void Awake() {
        moduleId = moduleIdCounter++;

        // Delegation
        RollButton.OnInteract += delegate () { RollButtonPressed();  return false; };

        for (int i = 0; i < SelectDice.Length; i++) {
            int j = i;
            SelectDice[i].OnInteract += delegate () { SelectDicePressed(j); return false; };
        }

        Module.OnActivate += OnActivate;
    }

    // Sets the selectable dice to random values at the start of the bomb
    private void Start() {
        for (int i = 0; i < numberIndexes.Length; i++) {
            numberIndexes[i] = UnityEngine.Random.Range(0, 6);
            SubmitDice[i].material = DiceMaterials[numberIndexes[i]];
            submittedValues[i] = numbers[numberIndexes[i]];
        }
    }

    private void OnActivate() {
        StartCoroutine(Roll(true));
    }


    // Roll button is pressed
    private void RollButtonPressed() {
        RollButton.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gameObject.transform);

        if (canRoll == true)
            StartCoroutine(Roll(false));
    }

    // Selectable dice is pressed
    private void SelectDicePressed(int i) {
        SelectDice[i].AddInteractionPunch(0.25f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, gameObject.transform);

        numberIndexes[i]++;

        // Checks boundaries
        if (numberIndexes[i] >= numbers.Length)
            numberIndexes[i] = 0;

        submittedValues[i] = numbers[numberIndexes[i]];
        SubmitDice[i].material = DiceMaterials[numberIndexes[i]];
    }


    // Rolls the dice
    private IEnumerator Roll(bool safe) {
        canRoll = false;

        if (safe == false)
            CheckCondition();

        Audio.PlaySoundAtTransform("EncDice_Roll", transform);

        for (int i = 0; i < 10; i++) {
            for (int j = 0; j < RollDice.Length; j++) {
                int number = UnityEngine.Random.Range(0, 6);
                RollDice[j].material = DiceMaterials[number];
                rolledValues[j] = numbers[number];
            }

            yield return new WaitForSeconds(0.1f);
        }

        GetAnswer();
        canRoll = true;
    }

    // Checks if the answer is correct
    private void CheckCondition() {
        bool correct = false;

        if ((submittedValues[0] == x && submittedValues[1] == y) ||
            (submittedValues[0] == y && submittedValues[1] == x))
            correct = true;

        if (correct == true) {
            Debug.LogFormat("[Encrypted Dice #{0}] You submitted the correct answer.", moduleId);

            if (stagesCompleted < 4)
                stagesCompleted++;

            // Module solved
            if (stagesCompleted == 3) {
                Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, gameObject.transform);
                GetComponent<KMBombModule>().HandlePass();
                solved = true;
                Debug.LogFormat("[Encrypted Dice #{0}] Module solved! Keep playing with the module if you want, it won't strike you anymore.", moduleId);
            }

            UpdateLEDs();
        }

        else {
            Debug.LogFormat("[Encrypted Dice #{0}] You submitted the wrong answer.", moduleId);
            StartCoroutine(StrikeLEDs());

            if (solved == false) {
                Debug.LogFormat("[Encrypted Dice #{0}] Strike!", moduleId);
                GetComponent<KMBombModule>().HandleStrike();
            }
        }
    }

    // Gets the answer to the dice roll
    private void GetAnswer() {
        int[] values = new int[3];

        for (int i = 0; i < rolledValues.Length; i++)
            values[i] = rolledValues[i];

        values = Sort(values);

        // Callibrates the values to the same terminology as the manual
        a = values[0];
        b = values[1];
        c = values[2];

        if (a == b && a == c) {
            x = 5;
            y = 5;
        }

        else if (a == b || b == c) {
            x = a;
            y = c;
        }

        else if (c == 6) {
            x = a;
            y = b;
        }

        else if (c == 5 && b - a == 1) {
            x = a;
            y = 6;
        }

        else if (c == 5) {
            x = a + b;
            y = 6;
        }

        else if (a != 1 && b != 1 && c != 1) {
            x = 1;
            y = 1;
        }

        else if (a != 2 && b != 2 && c != 2) {
            x = 2;
            y = 2;
        }

        else if (a != 3 && b != 3 && c != 3) {
            x = 3;
            y = 3;
        }

        else {
            x = 4;
            y = 4;
        }

        // Logs based on equal x and y values
        if (x == y)
            Debug.LogFormat("[Encrypted Dice #{0}] You rolled {1}, {2}, and {3}. The solution is {4}, {5}.", moduleId, rolledValues[0], rolledValues[1], rolledValues[2], x, y);

        else
            Debug.LogFormat("[Encrypted Dice #{0}] You rolled {1}, {2}, and {3}. The solution is {4}, {5}, or {5}, {4}.", moduleId, rolledValues[0], rolledValues[1], rolledValues[2], x, y);
    }

    // Sorts the rolled values
    private int[] Sort(int[] values) {

        // https://www.tutorialspoint.com/selection-sort-program-in-chash

        int temp, smallest;
        for (int i = 0; i < values.Length - 1; i++) {
            smallest = i;
            for (int j = i + 1; j < values.Length; j++) {
                if (values[j] < values[smallest]) {
                    smallest = j;
                }
            }
            temp = values[smallest];
            values[smallest] = values[i];
            values[i] = temp;
        }

        return values;
    }


    // Updates the LEDs
    private void UpdateLEDs() {
        switch(stagesCompleted) {
        case 0: {
            LEDS[0].material = LEDMaterials[0];
            LEDS[1].material = LEDMaterials[0];
            LEDS[2].material = LEDMaterials[0];
        }
        break;

        case 1: {
            LEDS[0].material = LEDMaterials[1];
            LEDS[1].material = LEDMaterials[0];
            LEDS[2].material = LEDMaterials[0];
        }
        break;

        case 2: {
            LEDS[0].material = LEDMaterials[1];
            LEDS[1].material = LEDMaterials[1];
            LEDS[2].material = LEDMaterials[0];
        }
        break;

        default: {
            LEDS[0].material = LEDMaterials[1];
            LEDS[1].material = LEDMaterials[1];
            LEDS[2].material = LEDMaterials[1];
        }
        break;
        }
    }

    // Makes the LEDs flash red on a strike
    private IEnumerator StrikeLEDs() {
        switch (stagesCompleted) {
        case 0: {
            LEDS[0].material = LEDMaterials[2];
            LEDS[1].material = LEDMaterials[2];
            LEDS[2].material = LEDMaterials[2];
        }
        break;

        case 1: {
            LEDS[0].material = LEDMaterials[3];
            LEDS[1].material = LEDMaterials[2];
            LEDS[2].material = LEDMaterials[2];
        }
        break;

        case 2: {
            LEDS[0].material = LEDMaterials[3];
            LEDS[1].material = LEDMaterials[3];
            LEDS[2].material = LEDMaterials[2];
        }
        break;

        default: {
            LEDS[0].material = LEDMaterials[3];
            LEDS[1].material = LEDMaterials[3];
            LEDS[2].material = LEDMaterials[3];
        }
        break;
        }

        yield return new WaitForSeconds(0.8f);
        UpdateLEDs();
    }
}
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class CreateFont : MonoBehaviour
{
    public GameObject ParentObject;
    public BoxCollider MyCollider;
    public GameObject ProxyToDestroy;
    private float offsetAmount = 0.065f;
    private float offset = 0.0f;
    private bool textEntered = false;
    private string text = "";
    private string currentText = "";
    private List<GameObject> letters = null;
    private TouchScreenKeyboard keyboard;
    private Vector3 colliderStartSize;

    void Start()
    {
        Destroy(ProxyToDestroy);
        colliderStartSize = MyCollider.size;
    }
    void Update()
    {
        if (keyboard != null)
        {
            if (keyboard.active)
            {
                // has the text changed
                if (keyboard.text != currentText)
                {
                    Debug.Log("User input is different : " + text);
                    //textEntered = true;
                    RedoText();
                }
            }

            if (keyboard != null && (keyboard.done || keyboard.wasCanceled) )
            {
				AREngine.instance.LastTextString = currentText;
                keyboard.text = null;
				keyboard = null;	
            }
        }
    }

    public void EnterText()
    {
        if (letters == null)
        {
            letters = new List<GameObject>();
        }

        if (keyboard == null)
        {
            Debug.Log("EnterText called");
			text = AREngine.instance.LastTextString;
			currentText = AREngine.instance.LastTextString;
            keyboard = TouchScreenKeyboard.Open(text, TouchScreenKeyboardType.ASCIICapable);
			TouchScreenKeyboard.hideInput = true;

            RedoText();
        }
    }
    void RedoText()
    {
        Debug.Log("Redid text");

        for (int count = 0; count < letters.Count; count++)
        {
            Destroy(letters[count]);
        }
        letters.Clear();

        CreateText();
    }

    void CreateText()
    {
        Debug.Log("Created text");

        if (keyboard != null)
        {
            currentText = keyboard.text;
        }

        offset = 0f;
        //MyCollider.size = colliderStartSize;

        string textToCreate = currentText.ToUpper();

        var chars = textToCreate.ToCharArray();


        // don't move if just one character
        if (chars.Length > 1)
        {
            offset = +(chars.Length * .025f);
        }

        foreach (char character in chars)
        {
		
            if (character != null && Resources.Load("Text/" + character) != null)
            {

                MyCollider.size = new Vector3(MyCollider.size.x + .025f, MyCollider.size.y, MyCollider.size.z);

                GameObject letterGo = Instantiate(Resources.Load("Text/" + character), new Vector3(0, 0, 0), Quaternion.identity) as GameObject;

                Debug.Log(letters);
                letters.Add(letterGo);

                letterGo.transform.parent = ParentObject.transform;
                letterGo.transform.localPosition = new Vector3(offset, 0f, 0);
                letterGo.transform.localScale = new Vector3(.01f, .01f, .01f);
                //letterGo.transform.localRotation = Quaternion.identity;
                //letterGo.transform.rotation = Quaternion.identity;

                letterGo.transform.localEulerAngles = new Vector3(0, 0, 0);
            }
            offset -= offsetAmount;
        }
    }
}

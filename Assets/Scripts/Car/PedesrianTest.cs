﻿using UnityEngine;
using System.Collections;

public class PedesrianTest : MonoBehaviour {

  private float Speed;

	// Use this for initialization
	void Start ()
  {
    Speed = 2.0f;
  }
	
	// Update is called once per frame
	void Update ()
  {
    Move();
    RayCast();
  }

  void Move()
  {
    if (Input.GetKey(KeyCode.A))
      transform.Translate(Vector3.left * Speed * Time.deltaTime);

    if (Input.GetKey(KeyCode.W))
      transform.Translate(Vector3.forward * Speed * Time.deltaTime);
        
    if (Input.GetKey(KeyCode.S))
      transform.Translate(Vector3.back * Speed * Time.deltaTime);

    if (Input.GetKey(KeyCode.D))
      transform.Translate(Vector3.right * Speed * Time.deltaTime);
  }

  void RayCast()
  {
    RaycastHit hit;
    if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 0.3f))
    {
      Debug.DrawRay(transform.position, transform.position + transform.TransformDirection(Vector3.forward) * 10.0f);
      if (hit.collider.gameObject.tag == "CrossWalk")
      {
        Debug.Log("Hola");
        hit.collider.gameObject.GetComponent<CrossWalkBehaviour>().SetIsPedestrianWaiting(true);
      }else
      {
        hit.collider.gameObject.GetComponent<CrossWalkBehaviour>().SetIsPedestrianWaiting(false);
      }
    }
  }
}
# DesktopSDView
Hacky window viewer for live stable diffusion generation

Use with a ComfyUI API Export, set the input image to same file as 'ScreenCap Image' on form, output image the same file as 'Return Image'. WAS SUITE recommended for loading and saving images to a filepath rather than internal Comfy folders.

Autosend will send a prompt at an interval adjusted by the slider, 'One Shot' will just send a prompt once.

![image](https://github.com/user-attachments/assets/e461d4bf-fb65-4fed-8f33-b465d7bfccf8)

For the ComfyUI workflow set the positive prompt to the text 'positiveprompt' and the negative prompt to 'negativeprompt', change the seed to 666999 if you want the seed to randomize each prompt sent.

Example workflow (Uses WAS and LCM lora)

![image](https://github.com/user-attachments/assets/1e61d3a7-071d-4bbc-bada-5aa1086f99ad)

![image](https://github.com/user-attachments/assets/099efcc3-e02d-4d8f-949f-de19265dbe84)

Export 'workflow_api.json' to folder containing EXE for now

Todo -
Save Images
Make resizable
Use HTTP to send and receive images
Check queue is empty before sending prompt
Add file pickers
Other stuff I can't think of right now

https://github.com/user-attachments/assets/e1bba9f3-ae32-46c2-b6db-643740c36a2f


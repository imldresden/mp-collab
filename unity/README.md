# Kinect Body Tracking Server Application

### Installation:

#### 1) The repository uses Git LFS for some large files.
Make sure that your system is configured correctly for this.

#### 2) Next make sure you have all the [required DLLs for ONNX Runtime execution](https://docs.microsoft.com/en-us/azure/kinect-dk/body-sdk-setup#required-dlls-for-onnx-runtime-execution-environments):

First, download and install [Visual C++ Redistributable](https://docs.microsoft.com/en-us/azure/kinect-dk/body-sdk-setup#visual-c-redistributable-for-visual-studio-2015).

Additionally:

**For DirectML (default)**:
* Copy the **directml.dll** from the sample_unity_bodytracking folder to the unity editor directory (e.g C:\Program Files\Unity\Hub\Editor\2019.1.2f1\Editor)

**For CUDA (untested)**:
* Download and install appropriate version of CUDA and make sure that CUDA_PATH exists as an environment variable (e.g C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v11.4).
* Download and install appropriate version of cuDNN and add a value to the PATH environment variable for it (e.g C:\Program Files\NVIDIA GPU Computing Toolkit\cuda-8.2.2.6\bin).

**For TensorRT (untested)**:
* Download and install appropriate version of CUDA and make sure that CUDA_PATH exists as an environment variable (e.g C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v11.4).
* Download and install appropriate version of TensorRT and add a value to the PATH environment variable for it (e.g C:\Program Files\NVIDIA GPU Computing Toolkit\TensorRT-8.2.1.8\lib).

#### If you wish to create a new scene:

* Create a gameobject and add the component for the main.cs script.
* Go to the prefab folder and drop in the Kinect4AzureTracker prefab.
* Now drag the gameobject for the Kinect4AzureTracker onto the Tracker slot in the main object in the inspector.


### Finally if you Build a Standalone Executable:

You will need to put [required DLLs for ONNX Runtime execution](https://docs.microsoft.com/en-us/azure/kinect-dk/body-sdk-setup#required-dlls-for-onnx-runtime-execution-environments) in the same directory with the .exe:

You can copy ONNXRuntime and DirectML files from nuget package by hand or from the main directory.

For the CUDA/cuDNN/TensorRT DLLs (Step #4) you can either have them in the PATH environment variable or copy required set of DLLs from the installation locations:

e.g. 
* from C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v11.4\bin for the CUDA files.
* from C:\Program Files\NVIDIA GPU Computing Toolkit\cuda-8.2.2.6\bin for the cuDNN files.
* from C:\Program Files\NVIDIA GPU Computing Toolkit\TensorRT-8.2.1.8\lib for the TensorRT files.

GCC-PHAT Time Delay Estimation
=============

Description
-----------
```gccphat.exe``` is a command-line tool for estimating the time delay, in milliseconds, versus time, between channels of a stereo signal using the Generalized Cross-Correlation Phase Transform (GCC-PHAT) technique, as described by Knapp and Carter in their IEEE article of 1976: https://ieeexplore.ieee.org/document/1162830

Features
---------
- Efficient FFT-based computation for cross-correlation.
- Robust time delay estimation using the phase transform.
- Optimized performance with precomputed exponential lookup tables.

Usage
-----
To estimate the time delays between the stereo channels, run the executable with the required parameters:

```
gccphat.exe <audioFilePath> <bufferSize> <fmin> <fmax>
```

- ```<audioFilePath>```: Path to the stereo audio file (WAV format).
- ```<bufferSize>```: Size of the buffer in samples (must be a power of two).
- ```<fmin>```: Minimum frequency in Hz for band-pass filtering.
- ```<fmax>```: Maximum frequency in Hz for band-pass filtering.

Example:
```
gccphat.exe stereo_noise.wav 1024 300 3000
```

Output:
The program will output a CSV file with the time delays between channels in milliseconds versus time in seconds. The file will be saved in the same directory as the input audio file.


Download Executable
---
You can download the executable directly from the link below:

[Download GCC-PHAT Executable](https://your-link-to-zip-file.com/gcc-phat.zip)



Build the solution
------------------

Follow these steps to set up and use the GCC-PHAT project:

1. **Clone the Repository:**
   
Clone the repository to your local machine using the following command:

```
git clone https://github.com/pmarmaroli/gccphat.git
```

Navigate to the project directory:

```
cd gccphat
```

2. **Open the Project in Visual Studio:**
- Open Visual Studio.
- Select `File` > `Open` > `Project/Solution`.
- Navigate to the `gccphat` directory and open the solution file (`gccphat.sln`).

3. **Build the Project:**
- In Visual Studio, build the solution by selecting `Build` > `Build Solution` or pressing `Ctrl+Shift+B`.

4. **Run the Application:**
- Open a Command Prompt or Terminal.
- Navigate to the output directory of the built executable. For a Debug build, it is typically:

  ```
  cd gccphat\bin\Debug
  ```


Acknowledgments
---------------

This project includes code for performing an in-place complex FFT, which was written by Gerald T. Beauregard. The original code is released under the MIT License.

Citation
--

If you use this code in your research or project, please cite it as follows:

```
@software{pmarmaroli_2024_gccphat,
  author = {Patrick Marmaroli},
  title = {GCC-PHAT Time Delay Estimation},
  year = {2024},
  url = {https://github.com/pmarmaroli/gccphat}
}
```

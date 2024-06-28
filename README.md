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
gccphat.exe <audioFilePath> <bufferSize> <fmin> <fmax> <outputToConsole>
```

- ```<audioFilePath>```: Path to the stereo audio file (WAV format).
- ```<bufferSize>```: Size of the buffer in samples (must be a power of two).
- ```<fmin>```: Minimum frequency in Hz for band-pass filtering.
- ```<fmax>```: Maximum frequency in Hz for band-pass filtering.
- ``` <outputMode> ```: Flag to output results to the console (if `console`) or to a CSV file (if `csv`).

Example:
```
gccphat.exe stereo_noise.wav 1024 300 3000 console
gccphat.exe stere_noise_.wav 2048 100 8000 csv
```

### How to choose the right buffer size ?

The buffer size determines the length of the window analysis and must be an integer, a power of two, and greater than the expected delay. The table below provides some examples:

| Signal Sampling Rate (Hz) | Maximum Expected Delay (ms) | Maximum Expected Delay (samples) | Minimum Buffer Size (next power of two) |
|----------------------------|-----------------------------|-----------------------------------|-----------------------------------------|
| 8000                       | 1                           | 8                                 | 16                                      |
| 8000                       | 5                           | 40                                | 64                                      |
| 8000                       | 10                          | 80                                | 128                                     |
| 16000                      | 1                           | 16                                | 32                                      |
| 16000                      | 5                           | 80                                | 128                                     |

#### Theoretical Formulas

1. **Maximum Expected Delay (samples):**
   ```
   Maximum Expected Delay (samples) = ceil((Signal Sampling Rate (Hz) * Maximum Expected Delay (ms)) / 1000)
   ````
   This formula converts the maximum expected delay from milliseconds to samples by multiplying the signal sampling rate (in Hz) by the maximum expected delay (in ms) and then dividing by 1000 (to convert ms to seconds). The result is rounded up to the nearest integer.

2. **Minimum Buffer Size (next power of two):**
   ```
   Minimum Buffer Size = 2^(ceil(log2(Maximum Expected Delay (samples))))
   ```
   This formula calculates the smallest power of two greater than or equal to the maximum expected delay in samples. The result is obtained by taking the base-2 logarithm of the maximum expected delay (samples), rounding up to the nearest integer, and then raising 2 to that power.

For more customized calculations, please visit our [buffer size calculator](https://pmarmaroli.github.io/bufferSizeCalculator.html).


Output:
----

- If `outputMode` is set to `console`, the program will print the time delays between channels in milliseconds directly to the console.
- If `outputMode` is set to `csv`, the program will output a CSV file with the time delays between channels in milliseconds versus time in seconds. The file will be saved in the same directory as the input audio file.

Sign convention
---
- a negative delay means channel 2 is delayed compared to channel 1
- a positive delay means channel 1 is delayed compared to channel 2

Example of Console Output:
---

```
> gccphat.exe stereo_noise.wav 8192 0 24000 true
Channels separated successfully.
Time Delay (ms) RMS Value
------------------------------
-10,020833333333332 6,140172267301812
-10,020833333333332 5,849366733674642
-10,020833333333332 6,216865357521091
-10,020833333333332 6,008303843811485
-10,020833333333332 5,485012392480706
-10,020833333333332 5,920044803823134
-10,020833333333332 5,592961057724904
...
Execution time: 0,062 seconds
```

Note the RMS (root mean square) output is calculated on first channel only, and considering the signal between `fmin` and `fmax` only.

Example of CSV Output:
---

```
Time (s);Time Delay (ms);RNS Value
0.000000;1.234;6.140
0.021333;2.345;5.849
0.042667;1.678;6.217
...
``` 

The CSV file will be saved as `<file_name>_timedelay_ms_vs_time_s.csv` in the same directory as the input audio file.

Download Executable
---
You can download the executable directly from the link below:

[Download the executable](https://github.com/pmarmaroli/gccphat/blob/main/gccphat.zip)


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

I would like to acknowledge [Ergo Esken](https://www.linkedin.com/in/ergo-esken) from [Microsoft Development Center Estonia](https://www.facebook.com/MSDevEstonia/) for his valuable feedbacks and beta-testing.

Citation
--

If you use this code in your research or project, please cite it as follows:

```
@software{pmarmaroli_2024_gccphat,
  author = {Patrick Marmaroli},
  title = {A C# implementation of the GCC-PHAT algorithm for time delay estimation},
  year = {2024},
  url = {https://github.com/pmarmaroli/gccphat}
}
```

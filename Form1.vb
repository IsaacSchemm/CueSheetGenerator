Imports System.IO
Imports NAudio.Wave

Public Class Form1
    Async Sub Form1_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        Dim inputFileByteLengths As New List(Of Long)
        Dim inputFileDurations As New List(Of TimeSpan)
        For Each inputFile In My.Application.CommandLineArgs
            Using reader As New MediaFoundationReader(inputFile)
                inputFileByteLengths.Add(reader.Length)
                inputFileDurations.Add(reader.TotalTime)
            End Using
        Next

        Dim segmentLength = TimeSpan.FromMinutes(5)
        Dim trackBoundaries As New List(Of TimeSpan)
        For Each inputFileDuration In inputFileDurations
            Dim ts = inputFileDuration
            While ts > segmentLength
                trackBoundaries.Add(segmentLength)
                ts -= segmentLength
            End While
            trackBoundaries.Add(ts)
        Next

        Using fs As New FileStream($"out.cue", FileMode.CreateNew, FileAccess.Write)
            Using sw As New StreamWriter(fs)
                Await sw.WriteLineAsync($"FILE out.wav WAVE")
                Dim track = 1
                Dim point = TimeSpan.Zero
                For Each trackBoundary In trackBoundaries
                    Dim totalSectors = CInt(point.TotalSeconds * 75)
                    Dim totalSeconds = totalSectors \ 75
                    Dim totalMinutes = totalSeconds \ 60
                    Dim seconds = totalSeconds - (totalMinutes * 60)
                    Dim sectors = totalSectors - (totalSeconds * 75)
                    Await sw.WriteLineAsync($"  TRACK {track:D2} AUDIO")
                    Await sw.WriteLineAsync($"    INDEX 01 {totalMinutes:D2}:{seconds:D2}:{sectors:D2}")
                    point += trackBoundary
                    track += 1
                Next
            End Using
        End Using

        ProgressBar1.Maximum = inputFileByteLengths.Sum()

        Using fs As New FileStream("out.wav", FileMode.CreateNew, FileAccess.Write)
            Dim buffer(33554432) As Byte
            Using combinedWriter As New WaveFileWriter(fs, New WaveFormat(44100, 2))
                For Each inputFile In My.Application.CommandLineArgs
                    Using reader As New MediaFoundationReader(inputFile)
                        Dim byteLength = reader.Length
                        Do
                            Dim bytesRead = Await reader.ReadAsync(buffer, 0, buffer.Length)
                            If bytesRead <= 0 Then
                                Exit Do
                            End If
                            ProgressBar1.Value += bytesRead
                            Await combinedWriter.WriteAsync(buffer, 0, bytesRead)
                        Loop
                    End Using
                Next
            End Using
        End Using

        Application.Exit()
    End Sub
End Class

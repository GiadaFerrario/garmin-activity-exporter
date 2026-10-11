using ActivityExporter.Core.Exceptions;
using ActivityExporter.Core.Interfaces;
using ActivityExporter.Core.Models;
using Dynastream.Fit;

namespace ActivityExporter.Api;

public class FitActivityParser : IActivityParser
{
    public ActivitySummary Parse(Stream stream)
    {
        // Decode reads the bytes; MesgBroadcaster dispatches each decoded message to the event of its type.
        // Created on every call: the handlers below accumulate state, which must not be shared between files.
        var decoder = new Decode();
        var broadcaster = new MesgBroadcaster();
        decoder.MesgEvent += broadcaster.OnMesg;
        decoder.MesgDefinitionEvent += broadcaster.OnMesgDefinition;

        // Messages are collected while decoding and mapped after Read() returns
        var sessions = new List<SessionMesg>();
        var laps = new List<LapMesg>();
        broadcaster.SessionMesgEvent += (sender, e) => sessions.Add((SessionMesg)e.mesg);
        broadcaster.LapMesgEvent += (sender, e) => laps.Add((LapMesg)e.mesg);

        try
        {
            // Read returns false (without throwing) when the integrity check of the file fails
            if (!decoder.Read(stream))
            {
                throw new InvalidFileException("The provided fit file is corrupted");
            }
        }
        catch (FitException ex)
        {
            throw new InvalidFileException("The provided fit file is not a valid fit file", ex);
        }
        
        if (sessions.Count > 1)
        {
            throw new InvalidFileException("The provided file contains more than one session");
        }
        // zero sessions
        if (sessions.Count == 0)
        {
            throw new InvalidFileException("The provided file contains no session");
        }
        
        return FitActivityMapper.Map(sessions[0], laps);
         
    }
}
